using HOSASBridge.App.Services;
using HOSASBridge.Infrastructure;
using HOSASBridge.DeviceHiding;
using HOSASBridge.Setup;
using System.Windows;

namespace HOSASBridge.App.ViewModels;

public sealed class DevicesViewModel : ObservableObject
{
    private readonly ProfileSession session;
    private readonly BridgeRuntime runtime;
    private readonly HidHideService hiding;
    private readonly Action<string> notify;
    private DeviceRole? identifying;
    private readonly Dictionary<Guid, double[]> baseline = [];
    private DateTime identifyDeadline;
    private Guid candidate;
    private int evidence;
    private string instructions = "First run: check dependencies, configure vJoy, then identify RIGHT and LEFT.";
    public string Instructions { get => L.T(instructions); private set => Set(ref instructions, value); }
    private string hidingStatus = "Not checked";
    public string HidingStatus { get => L.T(hidingStatus); private set => Set(ref hidingStatus, value); }
    public HidingHealth? Health { get; private set; }
    public ObservableCollection<string> Devices { get; } = [];
    public Array Roles { get; } = Enum.GetValues<DeviceRole>();
    public DeviceRole SelectedRole { get; set; } = DeviceRole.Right;
    public Command IdentifyRole { get; }
    public Command CopyDeviceInfo { get; }
    public Command IdentifyRight { get; }
    public Command IdentifyLeft { get; }
    public Command CancelIdentification { get; }
    public AsyncCommand Check { get; }
    public AsyncCommand InstallVJoy { get; }
    public AsyncCommand InstallHidHide { get; }
    public AsyncCommand RepairVirtual { get; }
    public AsyncCommand RepairHiding { get; }
    public AsyncCommand AllowApplication { get; }
    public AsyncCommand RestoreHiding { get; }
    public AsyncCommand FinishSetup { get; }
    public DevicesViewModel(ProfileSession session, BridgeRuntime runtime, HidHideService hiding, Action<Exception> error, Action<string> notify, Action finish)
    {
        this.session = session; this.runtime = runtime; this.hiding = hiding; this.notify = notify;
        IdentifyRole = new(() => Begin(SelectedRole));
        CopyDeviceInfo = new(() => { Clipboard.SetText(string.Join(Environment.NewLine, runtime.Devices.Select(d => $"{d.Manufacturer} {d.ProductName}; VID/PID={d.VendorId:X4}:{d.ProductId:X4}; axes={d.AxisCount}; buttons={d.ButtonCount}; POV={d.PovCount}; axes=[{d.AvailableAxes}]"))); notify("Controller information copied without instance identifiers."); });
        IdentifyRight = new(() => Begin(DeviceRole.Right)); IdentifyLeft = new(() => Begin(DeviceRole.Left));
        CancelIdentification = new(() => { identifying = null; runtime.StopIdentification(); Instructions = "Identification cancelled; stored roles unchanged."; });
        Check = new(async () => { await CheckHealthAsync(); notify("Status refreshed."); }, error);
        InstallVJoy = new(() => Install("vjoy"), error); InstallHidHide = new(() => Install("hidhide"), error);
        RepairVirtual = new(async () =>
        {
            await runtime.StopAsync();
            if (runtime.VirtualHealth.Ready) { notify("Virtual device already has the required capabilities."); return; }
            var answer = MessageBox.Show(L.F("Configure only vJoy Device {0} for HOSAS Bridge (8 axes, 32 buttons, 2 POVs)?\n\nThis can replace that device's existing configuration and interrupt a running game. Other vJoy devices are preserved. Choose a different ID in Mappings if this ID belongs to another application.", session.Current.VirtualDevice.DeviceId), L.T("Repair virtual device"), MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (answer != MessageBoxResult.OK) return;
            await SetupOperations.RunElevatedAsync("virtual", AppPaths.Profile, CancellationToken.None);
            notify("Virtual device configured. If Windows requests a reboot, restart before testing the game.");
            await CheckHealthAsync();
        }, error);
        RepairHiding = new(async () =>
        {
            await runtime.StopAsync();
            await SetupOperations.RunElevatedAsync("hiding", AppPaths.Profile, CancellationToken.None);
            await CheckHealthAsync(); notify("HidHide configuration saved. The current executable is allowed to read hidden devices.");
        }, error);
        AllowApplication = new(async () =>
        {
            await SetupOperations.RunElevatedAsync("allow", AppPaths.Profile, CancellationToken.None);
            await CheckHealthAsync(); notify("Current executable allowed. Device discovery refreshes automatically.");
        }, error);
        RestoreHiding = new(async () =>
        {
            await runtime.StopAsync();
            await SetupOperations.RunElevatedAsync("restore", AppPaths.Profile, CancellationToken.None);
            await CheckHealthAsync(); notify("HidHide device rules restored to their recorded pre-setup state. Shared drivers retained.");
        }, error);
        FinishSetup = new(async () =>
        {
            await CheckHealthAsync();
            if (session.Current.Devices.Count == 0 || session.Current.Axes.Any(a => !session.Current.Devices.ContainsKey(a.Role)) || !runtime.VirtualHealth.Ready || !IsHealthy) throw new InvalidOperationException("Complete device identification, virtual device setup and hiding repair first.");
            finish(); Instructions = "Setup complete. Use Diagnostics for the live axis test, then start the bridge.";
        }, error);
    }
    public bool IsHealthy => Health is { Installed: true, Whitelisted: true, Active: true, VirtualVisible: true, PolicyMatches: true };
    public async Task CheckHealthAsync()
    {
        Health = await hiding.CheckAsync(session.Current, Environment.ProcessPath!, CancellationToken.None);
        HidingStatus = L.F("Installed: {0}  ·  App allowed: {1}  ·  RIGHT hidden: {2}  ·  LEFT hidden: {3}  ·  Cloaking: {4}\n{5}", L.YesNo(Health.Installed), L.YesNo(Health.Whitelisted), L.YesNo(Health.RightHidden), L.YesNo(Health.LeftHidden), L.YesNo(Health.Active), L.T(Health.Detail));
        Changed(nameof(IsHealthy));
    }
    private async Task Install(string name)
    {
        await runtime.StopAsync();
        var result = await SetupOperations.RunElevatedAsync(name, null, CancellationToken.None);
        notify(result == 3010 ? "Driver installed. Windows restart is required." : "Driver installer finished. Check health, then configure the virtual device.");
        await CheckHealthAsync();
    }
    private void Begin(DeviceRole role)
    {
        runtime.Stop(); runtime.ObserveForIdentification(); identifying = role; baseline.Clear(); candidate = Guid.Empty; evidence = 0;
        foreach (var pair in runtime.IdentificationStates.Where(p => p.Value.Connected)) baseline[pair.Key] = pair.Value.Axes.ToArray();
        identifyDeadline = DateTime.UtcNow.AddSeconds(30);
        Instructions = L.F("Move only the {0} joystick through a large movement. Keep the other stick still. (30 seconds)", L.T(role.ToString()).ToUpperInvariant());
    }
    public void Tick()
    {
        var lines = runtime.Devices.Select(d => $"{d.Manufacturer} {d.ProductName} · {L.T("Connected")} · {d.VendorId:X4}:{d.ProductId:X4} · {L.T("Axes")}: {d.AxisCount} · {L.T("Buttons")}: {d.ButtonCount} · POV: {d.PovCount}\n{L.T("Role")}: {string.Join(", ", session.Current.Devices.Where(p => p.Value.InstanceGuid == d.InstanceGuid).Select(p => L.T(p.Key.ToString())))}\n{d.InstanceId}\nGUID {d.InstanceGuid} · Container {d.ContainerId} · {d.Location}").ToArray();
        if (!Devices.SequenceEqual(lines)) { Devices.Clear(); foreach (var line in lines) Devices.Add(line); }
        if (identifying is not { } role) return;
        if (DateTime.UtcNow > identifyDeadline) { identifying = null; Instructions = "No unambiguous movement detected. Select identification again and move one stick only."; return; }
        var moving = new List<Guid>();
        foreach (var (id, state) in runtime.IdentificationStates)
        {
            if (!state.Connected) continue;
            if (!baseline.TryGetValue(id, out var origin)) { baseline[id] = state.Axes.ToArray(); continue; }
            if (Enumerable.Range(0, state.Axes.Length).Any(i => Math.Abs(state.Axes[i] - origin[i]) > 13000)) moving.Add(id);
        }
        if (moving.Count != 1) { evidence = 0; return; }
        if (candidate == moving[0]) evidence++; else { candidate = moving[0]; evidence = 1; }
        if (evidence < 3) return;
        var device = runtime.Devices.Single(d => d.InstanceGuid == candidate);
        var assigned = session.Current.Devices.FirstOrDefault(p => p.Key != role && p.Value.InstanceGuid == candidate);
        if (assigned.Value is not null)
        { Instructions = L.F("This device is already {0}. Move the other physical stick; use Reset roles if you need to swap them.", L.T(assigned.Key.ToString())); evidence = 0; return; }
        var roles = new Dictionary<DeviceRole, DeviceIdentity>(session.Current.Devices) { [role] = device };
        identifying = null; runtime.StopIdentification(); session.Save(session.Current with { Devices = roles });
        Instructions = L.F("{0} identified: {1}. {2}", L.T(role.ToString()), device.ProductName, L.T(roles.Count == 2 ? "Next: Repair Device Hiding, test axes in Diagnostics and bind MINIGUN in Modes." : "Now identify the other joystick."));
    }
}
