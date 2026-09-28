using System.Windows;
using System.Windows.Media;
using HOSASBridge.Infrastructure;
namespace HOSASBridge.App.ViewModels;

public sealed class AxisPreviewViewModel(BridgeRuntime runtime, Action<string> notify) : ObservableObject
{
    private AxisRow? selected;
    private bool calibrating;
    private double min, max;
    public AxisRow? Selected { get => selected; set { if (Set(ref selected, value)) calibrating = false; } }
    private PointCollection points = [];
    public PointCollection Points { get => points; private set => Set(ref points, value); }
    private string live = "";
    public string Live { get => live; private set => Set(ref live, value); }
    public Command BeginCalibration => new(() => { if (Selected is null) return; calibrating = true; min = double.PositiveInfinity; max = double.NegativeInfinity; notify("Move the selected axis fully in both directions, then return it to center and finish calibration."); });
    public Command FinishCalibration => new(() =>
    {
        if (!calibrating || Selected is null) return;
        var raw = State()?.Axes[(int)Selected.Source]; calibrating = false;
        if (raw is null || !(min < raw && raw < max) || max - min < 10000) { notify("Calibration needs both endpoints and a neutral center. Try again."); return; }
        Selected.Min = min; Selected.Max = max; Selected.Center = raw.Value;
        notify("Calibration captured. Save axis settings to apply.");
    });
    private PhysicalState? State() => Selected is null ? null : runtime.Snapshot?.Roles.GetValueOrDefault(Selected.Role);
    public void Tick()
    {
        if (Selected is null) return;
        var state = State(); var raw = state?.Axes[(int)Selected.Source] ?? 32767.5;
        if (calibrating && state?.Connected == true) { min = Math.Min(min, raw); max = Math.Max(max, raw); }
        var settings = Selected.ToMapping().Transform;
        var transformer = new AxisTransformer();
        try
        {
            var normalized = AxisTransformer.Normalize(raw, settings.Calibration.Min, settings.Calibration.Center, settings.Calibration.Max);
            var processed = transformer.Transform(raw, settings);
            Live = L.F("Raw: {0:F0} · Normalized: {1:F2} · Processed: {2:F2} · Output: {3:F2}", raw, normalized, processed, runtime.Snapshot?.Output.Axes[(int)Selected.Target] ?? 0);
            var preview = settings with { Calibration = new(-1, 0, 1) };
            var curve = new PointCollection(65);
            for (var i = 0; i <= 64; i++) curve.Add(new Point(i * 5, 80 - transformer.Transform(i / 32.0 - 1, preview) * 76));
            curve.Freeze(); Points = curve;
        }
        catch (ArgumentException) { Live = L.T("Invalid axis settings. Review values before saving."); }
    }
}
