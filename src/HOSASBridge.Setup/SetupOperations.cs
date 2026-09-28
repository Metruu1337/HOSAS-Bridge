using System.Diagnostics;
using System.Security.Cryptography;
using HOSASBridge.Core;

namespace HOSASBridge.Setup;

public sealed record Dependency(string Name, string Version, string FileName, string Url, string Sha256, string Arguments);
public static class SetupOperations
{
    private static readonly SemaphoreSlim SetupGate = new(1, 1);
    public static readonly Dependency[] Dependencies =
    [
        new("vJoy", "2.2.2.0", "vJoySetup.exe", "https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe", "EF569A3105CD301B89580F18F60C66B339E95296ACF2C0DFCAF4B4BBF8AB68FE", "/NORESTART"),
        new("HidHide", "1.5.230.0", "HidHideSetup.exe", "https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe", "F4BBBCB82E6258641B887C74BC81C4C5F66E4AA811808DFC304347687B7605F6", "")
    ];
    public static async Task<int> RunElevatedAsync(string operation, string? argument, CancellationToken token)
    {
        if (!await SetupGate.WaitAsync(0, token)) throw new InvalidOperationException("A setup operation is already running. Finish it before starting another.");
        try
        {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas", WorkingDirectory = AppContext.BaseDirectory };
        start.ArgumentList.Add("--setup-operation"); start.ArgumentList.Add(operation);
        if (argument is not null) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Setup could not start.");
        await process.WaitForExitAsync(token).ConfigureAwait(false);
        if (process.ExitCode is not (0 or 3010)) throw new IOException($"Setup failed (exit {process.ExitCode}). See the setup error dialog and diagnostic log.");
        return process.ExitCode;
        } finally { SetupGate.Release(); }
    }
    public static async Task<int> InstallDependencyAsync(Dependency dependency, CancellationToken token)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "dependencies", dependency.FileName);
        if (!File.Exists(source)) throw new FileNotFoundException("The signed dependency package is missing. Reinstall HOSAS Bridge.", source);
        await using (var file = File.OpenRead(source))
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
            if (!hash.Equals(dependency.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Dependency checksum mismatch; installation refused.");
        }
        using var process = Process.Start(new ProcessStartInfo(source, dependency.Arguments) { UseShellExecute = false, CreateNoWindow = true }) ?? throw new IOException("Dependency installer failed to launch.");
        await process.WaitForExitAsync(token).ConfigureAwait(false);
        if (process.ExitCode is not (0 or 3010)) throw new IOException($"{dependency.Name} installer returned {process.ExitCode}.");
        return process.ExitCode;
    }
    public static string? FindVJoyConfig() => new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "vJoy", "x64", "vJoyConfig.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "vJoy", "vJoyConfig.exe")
    }.FirstOrDefault(File.Exists);
    public static async Task ConfigureVirtualAsync(VirtualRequirements requirements, bool replaceExisting, CancellationToken token)
    {
        var config = FindVJoyConfig() ?? throw new FileNotFoundException("vJoyConfig.exe is missing. Install the supported vJoy package.");
        var start = new ProcessStartInfo(config) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { requirements.DeviceId.ToString(System.Globalization.CultureInfo.InvariantCulture), "-a", "x", "y", "z", "rx", "ry", "rz", "sl0", "sl1", "-b", requirements.Buttons.ToString(System.Globalization.CultureInfo.InvariantCulture), "-p", requirements.Povs.ToString(System.Globalization.CultureInfo.InvariantCulture) }) start.ArgumentList.Add(arg);
        if (replaceExisting) start.ArgumentList.Add("-f");
        using var process = Process.Start(start) ?? throw new IOException("Could not start vJoy configuration.");
        var output = process.StandardOutput.ReadToEndAsync(token); var error = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token).ConfigureAwait(false);
        if (process.ExitCode != 0) throw new IOException($"vJoy configuration failed: {await output.ConfigureAwait(false)} {await error.ConfigureAwait(false)}");
    }
}
