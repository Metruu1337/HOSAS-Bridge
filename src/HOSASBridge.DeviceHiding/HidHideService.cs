using System.Diagnostics;
using System.Text.Json;
using HOSASBridge.Core;

namespace HOSASBridge.DeviceHiding;

public sealed class HidHideService : IDeviceHidingService
{
    public sealed record Ownership(Dictionary<string, bool> OriginalHidden, string[] AddedApplications);
    public string? OwnershipPath { get; init; }
    public async Task AllowApplicationAsync(string executable, CancellationToken token)
    {
        var cli = FindCli() ?? throw new FileNotFoundException("Install HidHide first.");
        var current = await RunAsync(cli, ["--app-list"], token).ConfigureAwait(false);
        var ownership = LoadOwnership();
        if (!current.Contains('"' + executable + '"', StringComparison.OrdinalIgnoreCase))
            SaveOwnership(ownership with { AddedApplications = ownership.AddedApplications.Append(executable).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() });
        await RunAsync(cli, ["--app-reg", executable], token).ConfigureAwait(false);
    }
    public static string? FindCli()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nefarius Software Solutions", "HidHide");
        return new[] { Path.Combine(root, "x64", "HidHideCLI.exe"), Path.Combine(root, "HidHideCLI.exe") }.FirstOrDefault(File.Exists);
    }
    public static async Task<string> RunAsync(string cli, IEnumerable<string> args, CancellationToken token)
    {
        var start = new ProcessStartInfo(cli) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start HidHide CLI.");
        var stdout = process.StandardOutput.ReadToEndAsync(token); var stderr = process.StandardError.ReadToEndAsync(token);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { if (!process.HasExited) process.Kill(); throw; }
        var output = await stdout.ConfigureAwait(false); var error = await stderr.ConfigureAwait(false);
        if (process.ExitCode != 0) throw new IOException($"HidHide CLI exit {process.ExitCode}: {error} {output}");
        return output;
    }
    public async Task<HidingHealth> CheckAsync(Profile profile, string executable, CancellationToken token)
    {
        var cli = FindCli();
        if (cli is null) return new(false, false, false, false, false, "HidHide is not installed.");
        try
        {
            var output = await RunAsync(cli, ["--app-list", "--dev-list", "--cloak-state", "--inv-state"], token).ConfigureAwait(false);
            bool Hidden(DeviceRole role) => profile.Devices.TryGetValue(role, out var d) && !string.IsNullOrEmpty(d.InstanceId) && output.Contains('"' + d.InstanceId + '"', StringComparison.OrdinalIgnoreCase);
            var allowed = output.Contains('"' + executable + '"', StringComparison.OrdinalIgnoreCase);
            var active = output.Contains("--cloak-on", StringComparison.Ordinal) && output.Contains("--inv-off", StringComparison.Ordinal);
            var right = Hidden(DeviceRole.Right); var left = Hidden(DeviceRole.Left);
            var virtualVisible = !output.Contains("VID_1234&", StringComparison.OrdinalIgnoreCase);
            var matches = profile.Devices.Keys.All(role => Hidden(role) == profile.Route(role).Hidden);
            var healthy = allowed && active && virtualVisible && matches;
            return new(true, allowed, right, left, active, healthy ? "Device hiding ready · persisted in Windows" : !virtualVisible ? "An existing HidHide rule hides vJoy. Restore that rule before using the bridge." : "Device hiding needs repair or joystick identification.", virtualVisible) { PolicyMatches = matches };
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception) { return new(true, false, false, false, false, ex.Message); }
    }
    public async Task RepairAsync(Profile profile, string executable, CancellationToken token)
    {
        var cli = FindCli() ?? throw new FileNotFoundException("Install HidHide first.");
        if (!File.Exists(executable) || !Path.IsPathFullyQualified(executable)) throw new InvalidDataException("Invalid application path.");
        var current = await RunAsync(cli, ["--inv-state", "--app-list", "--dev-list"], token).ConfigureAwait(false);
        if (current.Contains("--inv-on", StringComparison.Ordinal)) throw new InvalidOperationException("HidHide is using inverse mode for other software. Disable inverse mode before adopting HOSAS Bridge.");
        foreach (var role in profile.Devices.Keys)
            if (!profile.Devices.TryGetValue(role, out var selected) || !selected.InstanceId.StartsWith("HID\\", StringComparison.OrdinalIgnoreCase) || selected.VendorId == 0x1234)
                throw new InvalidDataException($"Identify the physical {role} joystick before applying hiding.");
        var ownership = LoadOwnership();
        var original = new Dictionary<string, bool>(ownership.OriginalHidden, StringComparer.OrdinalIgnoreCase);
        foreach (var d in profile.Devices.Values)
            original.TryAdd(d.InstanceId, current.Contains('"' + d.InstanceId + '"', StringComparison.OrdinalIgnoreCase));
        var apps = ownership.AddedApplications.ToList();
        if (!current.Contains('"' + executable + '"', StringComparison.OrdinalIgnoreCase) && !apps.Contains(executable, StringComparer.OrdinalIgnoreCase)) apps.Add(executable);
        SaveOwnership(new(original, apps.ToArray()));
        // Whitelist must be persisted before any device is hidden.
        await RunAsync(cli, ["--app-reg", executable], token).ConfigureAwait(false);
        var selectedIds = profile.Devices.Values.Select(d => d.InstanceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, wasHidden) in original.Where(p => !selectedIds.Contains(p.Key)))
            await RunAsync(cli, [wasHidden ? "--dev-hide" : "--dev-unhide", id], token).ConfigureAwait(false);
        foreach (var role in profile.Devices.Keys)
        {
            if (!profile.Devices.TryGetValue(role, out var device) || !device.InstanceId.StartsWith("HID\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Identify {role} using its Windows HID instance before hiding.");
            if (device.VendorId == 0x1234) throw new InvalidDataException("A virtual joystick cannot be hidden by HOSAS Bridge.");
            bool hide = profile.Route(role).Hidden;
            await RunAsync(cli, [hide ? "--dev-hide" : "--dev-unhide", device.InstanceId], token).ConfigureAwait(false);
        }
        await RunAsync(cli, ["--cloak-on"], token).ConfigureAwait(false);
        var check = await CheckAsync(profile, executable, token).ConfigureAwait(false);
        if (!check.Whitelisted || !check.Active || !check.VirtualVisible || !check.PolicyMatches)
            throw new IOException("HidHide verification failed after configuration.");
    }
    private Ownership LoadOwnership() => OwnershipPath is not null && File.Exists(OwnershipPath)
        ? JsonSerializer.Deserialize<Ownership>(File.ReadAllText(OwnershipPath)) ?? throw new InvalidDataException("HidHide ownership journal is corrupt.") : new([], []);
    private void SaveOwnership(Ownership data)
    {
        if (OwnershipPath is null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(OwnershipPath)!);
        File.WriteAllText(OwnershipPath + ".tmp", JsonSerializer.Serialize(data)); File.Move(OwnershipPath + ".tmp", OwnershipPath, true);
    }
    public async Task RestoreAsync(CancellationToken token)
    {
        var ownership = LoadOwnership(); var cli = FindCli();
        if (cli is null) return;
        foreach (var (id, hidden) in ownership.OriginalHidden)
            await RunAsync(cli, [hidden ? "--dev-hide" : "--dev-unhide", id], token).ConfigureAwait(false);
        foreach (var executable in ownership.AddedApplications)
            await RunAsync(cli, ["--app-unreg", executable], token).ConfigureAwait(false);
        if (OwnershipPath is not null && File.Exists(OwnershipPath)) File.Delete(OwnershipPath);
    }
}
