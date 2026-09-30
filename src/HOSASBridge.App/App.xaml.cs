using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using HOSASBridge.App.Services;
using HOSASBridge.App.ViewModels;
using HOSASBridge.DeviceHiding;
using HOSASBridge.Diagnostics;
using HOSASBridge.Infrastructure;
using HOSASBridge.Input.DirectInput;
using HOSASBridge.Output.VJoy;
using HOSASBridge.Profiles;
using HOSASBridge.Setup;

namespace HOSASBridge.App;

public partial class App : Application
{
    private void LocalizeGrid(object sender, RoutedEventArgs e)
    {
        var grid = (System.Windows.Controls.DataGrid)sender;
        if (!grid.AutoGenerateColumns) return;
        for (var i = 0; i < grid.Columns.Count; i++)
        {
            var column = grid.Columns[i]; var name = column.Header?.ToString() ?? "";
            column.Header = L.T(name); column.Width = new System.Windows.Controls.DataGridLength(1, System.Windows.Controls.DataGridLengthUnitType.Star);
            if (name != "Role") continue;
            grid.Columns[i] = new System.Windows.Controls.DataGridTextColumn
            {
                Header = L.T(name), Width = new System.Windows.Controls.DataGridLength(1, System.Windows.Controls.DataGridLengthUnitType.Star),
                Binding = new System.Windows.Data.Binding(name) { Converter = new LocalizedValueConverter(), ValidatesOnExceptions = true }
            };
        }
    }
    private SingleInstance? single;
    private BridgeRuntime? runtime;
    private MainViewModel? model;
    private TrayService? tray;
    private DiagnosticLog? log;
    private bool exiting;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var language = new SettingsStore().Load().Language;
            var culture = System.Globalization.CultureInfo.GetCultureInfo(language == "en" ? "en-US" : "pl-PL");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            L.LoadResources(Resources);
            log = new DiagnosticLog(AppPaths.Logs);
            log.Write("Information", "startup", $"HOSAS Bridge {BuildInfo.Version}; commit={BuildInfo.Commit}; {Environment.OSVersion}; .NET {Environment.Version}");
            if (e.Args.FirstOrDefault() == "--setup-operation") { await SetupAsync(e.Args); return; }
            single = new SingleInstance(e.Args.FirstOrDefault() == "--smoke-test" ? "ui-smoke-" + Environment.ProcessId : null);
            if (!single.IsFirst) { await single.NotifyAsync(); Shutdown(); return; }
            var repository = new ProfileRepository(log); var profile = repository.Load(AppPaths.Profile);
            if (!File.Exists(AppPaths.Profile)) repository.Save(AppPaths.Profile, profile);
            var window = new MainWindow(); MainWindow = window;
            var handle = new WindowInteropHelper(window).EnsureHandle();
            runtime = new(new ControllerInputProvider(new DirectInputProvider(handle, log), new XInputProvider(new WindowsXInputApi())), new VJoyAdapter(), profile, log);
            var session = new ProfileSession(repository, profile, runtime);
            model = new MainViewModel(runtime, session, log); window.DataContext = model;
            tray = new TrayService(window, model, ExitApplication);
            window.Closing += (_, args) =>
            {
                if (!exiting && model.Settings.Current.CloseToTray) { args.Cancel = true; window.Hide(); }
                else if (!exiting) { args.Cancel = true; ExitApplication(); }
            };
            _ = ListenAsync(window);
            DispatcherUnhandledException += (_, args) => { model.Error(args.Exception); args.Handled = true; };
            Microsoft.Win32.SystemEvents.PowerModeChanged += PowerModeChanged;
            await model.Devices.CheckHealthAsync();
            if (!model.Settings.Current.SetupCompleted || !model.Devices.IsHealthy) model.SelectedTab = 4;
            if (!model.Settings.Current.StartMinimized || !model.Settings.Current.SetupCompleted || !model.Devices.IsHealthy) window.Show();
            if (e.Args.FirstOrDefault() == "--smoke-test")
            {
                window.Show(); await UiSmokeTest.RunAsync(window, model, e.Args.ElementAtOrDefault(1) ?? "artifacts/ui-smoke"); ExitApplication(); return;
            }
            if (model.Settings.Current.StartBridgeAutomatically)
            { try { await model.StartAsync(); } catch (Exception ex) { model.Error(ex); window.Show(); } }
        }
        catch (Exception ex)
        {
            log?.Write("Error", "startup.failed", ex.Message, ex);
            MessageBox.Show(L.T(ex.Message), "HOSAS Bridge", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1);
        }
    }
    private async Task ListenAsync(Window window)
    {
        try { await single!.ListenAsync(() => Dispatcher.BeginInvoke(() => { window.Show(); window.WindowState = WindowState.Normal; window.Activate(); })); }
        catch (Exception ex) { log?.Write("Error", "instance.listen", ex.Message, ex); }
    }
    private bool resumeBridge;
    private void PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs args)
    {
        if (args.Mode == Microsoft.Win32.PowerModes.Suspend) { resumeBridge = model?.IsRunning == true; runtime?.Stop(); }
        if (args.Mode == Microsoft.Win32.PowerModes.Resume && resumeBridge) runtime?.Start();
    }
    private async Task SetupAsync(string[] args)
    {
        var operation = args.ElementAtOrDefault(1) ?? throw new InvalidDataException("Missing setup operation.");
        int result = 0;
        if (operation is "vjoy" or "hidhide") result = await SetupOperations.InstallDependencyAsync(SetupOperations.Dependencies[operation == "vjoy" ? 0 : 1], CancellationToken.None);
        else
        {
            var path = args.ElementAtOrDefault(2) ?? throw new InvalidDataException("Missing configuration path.");
            var hiding = new HidHideService { OwnershipPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "hiding-ownership.json") };
            if (operation == "restore") { await hiding.RestoreAsync(CancellationToken.None); Shutdown(); return; }
            if (operation == "allow") { await hiding.AllowApplicationAsync(Environment.ProcessPath!, CancellationToken.None); Shutdown(); return; }
            var profile = ProfileRepository.LoadStrict(path);
            if (operation == "hiding") await hiding.RepairAsync(profile, Environment.ProcessPath!, CancellationToken.None);
            else if (operation == "virtual")
            {
                await SetupOperations.ConfigureVirtualAsync(profile.VirtualDevice, true, CancellationToken.None);
                using var output = new VJoyAdapter();
                var health = output.Check(profile.VirtualDevice);
                if (!health.Ready) throw new IOException("Virtual device configuration needs verification or a Windows restart: " + health.Detail);
            }
            else throw new InvalidDataException("Unknown setup operation.");
        }
        Shutdown(result);
    }
    private void ExitApplication() { if (exiting) return; exiting = true; Shutdown(); }
    protected override void OnExit(ExitEventArgs e)
    {
        Microsoft.Win32.SystemEvents.PowerModeChanged -= PowerModeChanged;
        model?.Dispose(); runtime?.Dispose(); tray?.Dispose(); single?.Dispose();
        log?.Write("Information", "shutdown", "Virtual output released."); base.OnExit(e);
    }
}
