using HOSASBridge.App.Services;
using HOSASBridge.Diagnostics;
using HOSASBridge.Infrastructure;
using Microsoft.Win32;
using System.Diagnostics;

namespace HOSASBridge.App.ViewModels;

public sealed class LiveAxis : ObservableObject
{
    public string Name { get; }
    public DeviceRole Role { get; }
    public PhysicalAxis Source { get; }
    public LiveAxis(DeviceRole role, PhysicalAxis source) { Role = role; Source = source; Name = L.T(role.ToString()) + " · " + L.T(source.ToString()); }
    private double raw, normalized, transformed, output;
    public double Raw { get => raw; set => Set(ref raw, value); }
    public double Normalized { get => normalized; set => Set(ref normalized, value); }
    public double Transformed { get => transformed; set => Set(ref transformed, value); }
    public double Output { get => output; set => Set(ref output, value); }
    private string destination = "";
    public string Destination { get => destination; set => Set(ref destination, value); }
}
public sealed class DiagnosticsViewModel : ObservableObject
{
    private readonly ProfileSession session;
    private readonly BridgeRuntime runtime;
    private readonly AxisTransformer transformer = new();
    public ObservableCollection<LiveAxis> Axes { get; } = [];
    private string buttons = "", hats = "", performance = "";
    public string Buttons { get => buttons; private set => Set(ref buttons, value); }
    public string Hats { get => hats; private set => Set(ref hats, value); }
    public string Performance { get => performance; private set => Set(ref performance, value); }
    public AsyncCommand TestOutput { get; }
    public AsyncCommand Export { get; }
    public AsyncCommand OpenControllers { get; }
    public AsyncCommand OpenLogs { get; }
    public DiagnosticsViewModel(ProfileSession session, BridgeRuntime runtime, DiagnosticLog log, Func<UserSettings> settings, Action<Exception> error)
    {
        this.session = session; this.runtime = runtime;
        void Reload() { Axes.Clear(); foreach (var map in session.Current.Axes.Select(m => (m.Role, m.Source)).Distinct()) Axes.Add(new(map.Role, map.Source)); }
        session.Changed += Reload; Reload();
        OpenLogs = new(() => { Directory.CreateDirectory(AppPaths.Logs); Process.Start(new ProcessStartInfo(AppPaths.Logs) { UseShellExecute = true }); return Task.CompletedTask; }, error);
        TestOutput = new(() => runtime.TestOutputAsync(CancellationToken.None), error);
        OpenControllers = new(() => { Process.Start(new ProcessStartInfo("control.exe", "joy.cpl") { UseShellExecute = true }); return Task.CompletedTask; }, error);
        Export = new(() =>
        {
            var dialog = new SaveFileDialog { Filter = "Diagnostic ZIP (*.zip)|*.zip", FileName = $"HOSAS-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip" };
            if (dialog.ShowDialog() == true)
            {
                // Export only app-owned settings. Logs contain device identity but no process inventory or account credentials.
                if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
                log.Export(dialog.FileName, settings(), session.Current with { Devices = [], ProcessName = null }, runtime.Devices.Select(d => new { d.ProductName, d.Manufacturer, d.VendorId, d.ProductId, d.AxisCount, d.ButtonCount, d.PovCount }), new { App = BuildInfo.Version, Commit = BuildInfo.Commit, Tag = BuildInfo.Tag, OS = Environment.OSVersion.VersionString, VJoy = "2.2.2.0", HidHide = "1.5.230.0", DirectInput = "3.8.3", Runtime = Environment.Version.ToString() });
            }
            return Task.CompletedTask;
        }, error);
    }
    public void Tick()
    {
        var snapshot = runtime.Snapshot; if (snapshot is null) return;
        var modes = new ModeController(session.Current); if (session.Current.Modes.Any(m => m.Id == snapshot.Mode)) modes.Select(snapshot.Mode);
        foreach (var row in Axes)
        {
            var state = snapshot.Roles.GetValueOrDefault(row.Role); if (state is null) continue;
            row.Raw = state.Axes[(int)row.Source];
            var mapping = session.Current.Axes.FirstOrDefault(m => m.Role == row.Role && m.Source == row.Source);
            var transform = mapping is null ? new AxisTransform() : modes.TransformFor(mapping.Target) ?? mapping.Transform;
            row.Normalized = AxisTransformer.Normalize(row.Raw, transform.Calibration.Min, transform.Calibration.Center, transform.Calibration.Max);
            row.Transformed = mapping is not null && state.Connected ? transformer.Transform(row.Raw, transform) * (modes.Inverts(mapping.Target) ? -1 : 1) : 0;
            var routed = mapping is not null && session.Current.Route(row.Role).Virtualized;
            row.Output = routed ? snapshot.Output.Axes[(int)mapping!.Target] : 0;
            row.Destination = routed ? mapping!.Target.ToString() : L.T("Native · not virtualized");
        }
        static string Pressed(bool[] values) => string.Join(" ", values.Select((v, i) => (v, i)).Where(p => p.v).Select(p => p.i + 1));
        Buttons = L.F("RIGHT: [{0}]   LEFT: [{1}]\nVIRTUAL: [{2}]", Pressed(snapshot.Right.Buttons), Pressed(snapshot.Left.Buttons), Pressed(snapshot.Output.Buttons));
        Hats = L.F("RIGHT POV: {0}   LEFT POV: {1}   VIRTUAL: {2}  (−1 = neutral)", snapshot.Right.Povs[0], snapshot.Left.Povs[0], string.Join(", ", snapshot.Output.Povs.Take(2)));
        Performance = L.F("Input {0:F0} Hz   ·   Output {1:F0} Hz   ·   Processing {2:F3} ms (excludes USB/game latency)", snapshot.InputHz, snapshot.OutputHz, snapshot.ProcessingMs);
    }
}
