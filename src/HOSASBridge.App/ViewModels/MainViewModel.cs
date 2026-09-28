using HOSASBridge.App.Services;
using HOSASBridge.DeviceHiding;
using HOSASBridge.Diagnostics;
using HOSASBridge.Infrastructure;
using HOSASBridge.Profiles;
using System.Diagnostics;
using System.Windows.Threading;

namespace HOSASBridge.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly BridgeRuntime runtime;
    private readonly DiagnosticLog log;
    private readonly ProfileSession session;
    private readonly DispatcherTimer timer;
    private long nextProcessCheck, nextHealthCheck;
    private bool checkingHealth, gameWasRunning;
    private string lastMode = "";
    public SetupWizardViewModel Wizard { get; }
    public AboutViewModel About { get; }
    public ProfileViewModel Profile { get; }
    public ModesViewModel Modes { get; }
    public DevicesViewModel Devices { get; }
    public DiagnosticsViewModel Diagnostics { get; }
    public SettingsViewModel Settings { get; }
    public AsyncCommand Start { get; }
    public Command Stop { get; }
    public Command Normal { get; }
    public Command Minigun { get; }
    public Command RunSetup { get; }
    public Command TestInputs { get; }
    public Command Calibrate { get; }
    public AsyncCommand ResetRoles { get; }
    public event Action<string>? ModeChanged;
    private string message = "Ready", bridge = "STOPPED", mode = "NORMAL", left = "Not identified", right = "Not identified", virtualStatus = "Checking…";
    private int selectedTab;
    public int SelectedTab { get => selectedTab; set => Set(ref selectedTab, value); }
    public string Message { get => L.T(message); private set => Set(ref message, value); }
    public string Bridge { get => L.T(bridge); private set => Set(ref bridge, value); }
    public string Mode { get => mode; private set => Set(ref mode, value); }
    public string Left { get => L.T(left); private set => Set(ref left, value); }
    public string Right { get => L.T(right); private set => Set(ref right, value); }
    public string VirtualStatus { get => L.T(virtualStatus); private set => Set(ref virtualStatus, value); }
    public string BaseModeLabel => L.T(session.Current.Modes[0].Id);
    public string ActiveModeLabel => L.T(session.Current.ModeBinding?.ActiveMode ?? session.Current.Modes.Last().Id);
    public string ProfileName => session.Current.DisplayName;
    public string ProfileLabel => ((System.Globalization.CultureInfo.DefaultThreadCurrentUICulture ?? System.Globalization.CultureInfo.CurrentUICulture).TwoLetterISOLanguageName == "pl" ? "Profil: " : "Profile: ") + ProfileName;
    public bool IsRunning => runtime.Snapshot?.Running == true;
    public MainViewModel(BridgeRuntime runtime, ProfileSession session, DiagnosticLog log)
    {
        this.runtime = runtime; this.session = session; this.log = log;
        Settings = new(new SettingsStore(), new WindowsStartupService(), Error, Notify);
        Wizard = new(session, Error, Notify); About = new(Error);
        Profile = new(session, runtime, Error, Notify); Modes = new(session, runtime, Error);
        Devices = new(session, runtime, new HidHideService(), Error, Notify, Settings.CompleteSetup);
        Diagnostics = new(session, runtime, log, () => Settings.Current, Error);
        Start = new(StartAsync, Error); Stop = new(runtime.Stop);
        Normal = new(() => runtime.SelectMode(session.Current.Modes[0].Id));
        Minigun = new(() => runtime.SelectMode(session.Current.ModeBinding?.ActiveMode ?? session.Current.Modes.Last().Id));
        TestInputs = new(() => SelectedTab = 5); Calibrate = new(() => SelectedTab = 2);
        RunSetup = new(() => { Settings.ResetSetup(); Wizard.Reset(); SelectedTab = 4; });
        ResetRoles = new(async () => { await runtime.StopAsync(); session.Save(session.Current with { Devices = [] }); Notify("Roles cleared. Identify RIGHT, then LEFT. Previously hidden devices remain hidden until repaired or unhidden in HidHide."); }, Error);
        session.Changed += () => { Changed(nameof(BaseModeLabel)); Changed(nameof(ActiveModeLabel)); Changed(nameof(ProfileName)); Changed(nameof(ProfileLabel)); nextHealthCheck = 0; };
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => Tick(), Dispatcher.CurrentDispatcher);
        if (!Settings.Current.SetupCompleted) SelectedTab = 4;
    }
    public async Task StartAsync()
    {
        if (session.Current.Devices.Count == 0 || session.Current.Axes.Any(a => !session.Current.Devices.ContainsKey(a.Role))) throw new InvalidOperationException("Assign all mapped controller roles on the Devices tab.");
        await Devices.CheckHealthAsync();
        if (!Devices.IsHealthy) throw new InvalidOperationException("Repair Device Hiding before starting the bridge.");
        runtime.Start(); Notify("Bridge requested. Existing vJoy is acquired without recreating the device.");
    }
    public void Notify(string text) => Message = text;
    public void Error(Exception ex) { Message = ex.Message; log.Write("Error", "ui.operation", ex.Message, ex); }
    private async void Tick()
    {
        try
        {
            var snapshot = runtime.Snapshot;
            if (snapshot is not null)
            {
                Bridge = snapshot.Status; Mode = snapshot.Mode;
                Right = snapshot.Right.Connected ? "Connected" : "Disconnected / identify RIGHT";
                Left = snapshot.Left.Connected ? "Connected" : "Disconnected / identify LEFT";
                if (Mode != lastMode) { if (lastMode.Length > 0) ModeChanged?.Invoke(Mode); lastMode = Mode; }
            }
            VirtualStatus = runtime.VirtualHealth.Detail;
            Devices.Tick(); Modes.Tick(); Diagnostics.Tick(); Profile.Tuning.Tick();
            log.MinimumLevel = Settings.Current.LogLevel;
            var now = Environment.TickCount64;
            if (!checkingHealth && now > nextHealthCheck)
            {
                checkingHealth = true; nextHealthCheck = now + 10000;
                try { await Devices.CheckHealthAsync(); if (IsRunning && !Devices.IsHealthy) { runtime.Stop(); Notify("Repair Device Hiding before starting the bridge."); } } finally { checkingHealth = false; }
            }
            if (now > nextProcessCheck)
            {
                nextProcessCheck = now + 2000;
                if (Settings.Current.AutoActivateOnProcess && session.Current.ProcessName is { Length: > 0 } processName)
                {
                    var processes = Process.GetProcessesByName(processName); bool running = processes.Length > 0;
                    foreach (var process in processes) process.Dispose();
                    if (running && !gameWasRunning && !IsRunning) await StartAsync();
                    gameWasRunning = running;
                }
            }
        }
        catch (Exception ex) { Error(ex); }
    }
    public void Dispose() => timer.Stop();
}
