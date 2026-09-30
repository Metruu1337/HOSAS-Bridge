using HOSASBridge.App.Services;
using HOSASBridge.Profiles;
using Microsoft.Win32;

namespace HOSASBridge.App.ViewModels;

public sealed class AxisRow(AxisMapping mapping, DeviceIdentity? device = null)
{
    public string Meaning { get; set; } = mapping.Meaning;
    public string FriendlySource => L.T(PresetCatalog.AxisLabel(device, Source));
    public string Label => L.T(Role.ToString()) + " · " + FriendlySource;
    public DeviceRole Role { get; set; } = mapping.Role;
    public PhysicalAxis Source { get; set; } = mapping.Source;
    public VirtualAxis Target { get; set; } = mapping.Target;
    public bool Invert { get; set; } = mapping.Transform.Invert;
    public double Deadzone { get; set; } = mapping.Transform.CenterDeadzone;
    public double Outer { get; set; } = mapping.Transform.OuterDeadzone;
    public double Sensitivity { get; set; } = mapping.Transform.Sensitivity;
    public CurveKind Curve { get; set; } = mapping.Transform.Curve;
    public double Exponent { get; set; } = mapping.Transform.Exponent;
    public double Saturation { get; set; } = mapping.Transform.Saturation;
    public double Min { get; set; } = mapping.Transform.Calibration.Min;
    public double Center { get; set; } = mapping.Transform.Calibration.Center;
    public double Max { get; set; } = mapping.Transform.Calibration.Max;
    public AxisMapping ToMapping() => new(Role, Source, Target, new AxisTransform
    { Invert = Invert, CenterDeadzone = Deadzone, OuterDeadzone = Outer, Sensitivity = Sensitivity, Curve = Curve, Exponent = Exponent, Saturation = Saturation, Calibration = new(Min, Center, Max) }) { Meaning = Meaning };
}
public sealed class ButtonRow
{
    public DeviceRole Role { get; set; }
    public int Source { get; set; } = 1;
    public int Target { get; set; } = 1;
}
public sealed class PovRow
{
    public DeviceRole Role { get; set; }
    public int Source { get; set; } = 1;
    public int Target { get; set; } = 1;
}
public sealed class RoutingRow
{
    public DeviceRole Role { get; set; }
    public bool Native { get; set; }
    public bool Hidden { get; set; }
    public bool Virtualized { get; set; }
}
public sealed class ProfileViewModel : ObservableObject
{
    private readonly ProfileSession session;
    public AxisPreviewViewModel Tuning { get; }
    public bool CustomizeRoutes { get; set; }
    public ObservableCollection<RoutingRow> RoleRoutes { get; } = [];
    public ObservableCollection<AxisRow> Axes { get; } = [];
    public ObservableCollection<ButtonRow> Buttons { get; } = [];
    public ObservableCollection<PovRow> Povs { get; } = [];
    public Array Routings { get; } = Enum.GetValues<RoutingStrategy>();
    public Array Roles { get; } = Enum.GetValues<DeviceRole>();
    public Array VirtualAxes { get; } = Enum.GetValues<VirtualAxis>();
    public Array Curves { get; } = Enum.GetValues<CurveKind>();
    public string Name { get; set; } = "";
    public RoutingStrategy Routing { get; set; }
    public string ProcessName { get; set; } = "";
    public int DeviceId { get; set; } = 1;
    public AsyncCommand Save { get; }
    public AsyncCommand Import { get; }
    public AsyncCommand Export { get; }
    public AsyncCommand NewProfile { get; }
    public AsyncCommand Duplicate { get; }
    public AsyncCommand ResetPreset { get; }
    public Command AddAxis { get; }
    public Command SelectProcess { get; }
    public ProfileViewModel(ProfileSession session, HOSASBridge.Infrastructure.BridgeRuntime runtime, Action<Exception> error, Action<string> notify)
    {
        this.session = session; Tuning = new(runtime, notify);
        AddAxis = new(() => Axes.Add(new(new AxisMapping(DeviceRole.Other, PhysicalAxis.X, VirtualAxis.Rx, new()))));
        Save = new(() => { Apply(); notify("Profile saved and activated."); return Task.CompletedTask; }, error);
        Import = new(() =>
        {
            var dialog = new OpenFileDialog { Filter = "HOSAS profile (*.json)|*.json" };
            if (dialog.ShowDialog() == true) session.Save(ProfileRepository.Parse(File.ReadAllText(dialog.FileName)) with { Devices = session.Current.Devices });
            return Task.CompletedTask;
        }, error);
        Export = new(() =>
        {
            Apply(); var dialog = new SaveFileDialog { Filter = "HOSAS profile (*.json)|*.json", FileName = session.Current.ProfileId + ".json" };
            if (dialog.ShowDialog() == true) ProfileRepository.Export(dialog.FileName, session.Current);
            return Task.CompletedTask;
        }, error);
        Duplicate = new(() => { session.Save(session.Current with { ProfileId = Guid.NewGuid().ToString("N"), DisplayName = Name + " " + L.T("Copy") }); return Task.CompletedTask; }, error);
        ResetPreset = new(() => { var preset = PresetCatalog.IsGamepad(session.Current) ? PresetCatalog.Create(session.Current.ProfileId == "xbox-gamepad" ? "Xbox gamepad" : "PlayStation gamepad") : ProfileDefaults.Wardogs(Routing); session.Save(preset with { Devices = session.Current.Devices, VirtualDevice = session.Current.VirtualDevice }); return Task.CompletedTask; }, error);
        NewProfile = new(() => { session.Save(ProfileDefaults.Wardogs() with { ProfileId = Guid.NewGuid().ToString("N"), DisplayName = "Custom profile", Devices = session.Current.Devices }); return Task.CompletedTask; }, error);
        SelectProcess = new(() => { var dialog = new OpenFileDialog { Filter = "Game executable (*.exe)|*.exe" }; if (dialog.ShowDialog() == true) { ProcessName = Path.GetFileNameWithoutExtension(dialog.FileName); Changed(nameof(ProcessName)); } });
        session.Changed += Reload; Reload();
    }
    private void Reload()
    {
        var p = session.Current;
        CustomizeRoutes = p.RoleRoutes.Count > 0; RoleRoutes.Clear();
        foreach (var role in Enum.GetValues<DeviceRole>()) { var route = p.Route(role); RoleRoutes.Add(new() { Role = role, Native = route.Native, Hidden = route.Hidden, Virtualized = route.Virtualized }); }
        Changed(nameof(CustomizeRoutes));
        Name = p.DisplayName; Routing = p.Routing; ProcessName = p.ProcessName ?? ""; DeviceId = (int)p.VirtualDevice.DeviceId;
        Axes.Clear(); foreach (var a in p.Axes) Axes.Add(new(a, p.Devices.GetValueOrDefault(a.Role)));
        Tuning.Selected = Axes.FirstOrDefault();
        Buttons.Clear(); foreach (var b in p.Buttons) Buttons.Add(new() { Role = b.Role, Source = b.Source, Target = b.Target });
        Povs.Clear(); foreach (var h in p.Povs) Povs.Add(new() { Role = h.Role, Source = h.Source, Target = h.Target });
        Changed(nameof(Name)); Changed(nameof(Routing)); Changed(nameof(ProcessName)); Changed(nameof(DeviceId));
    }
    public void Apply() => session.Save(session.Current with
    {
        DisplayName = Name, Routing = Routing, RoleRoutes = CustomizeRoutes ? RoleRoutes.ToDictionary(r => r.Role, r => new RoleRouting(r.Native, r.Hidden, r.Virtualized)) : [], ProcessName = string.IsNullOrWhiteSpace(ProcessName) ? null : ProcessName.Trim(),
        Axes = Axes.Select(a => a.ToMapping()).ToArray(), Buttons = Buttons.Select(b => new ButtonMapping(b.Role, b.Source, b.Target)).ToArray(),
        Povs = Povs.Select(h => new PovMapping(h.Role, h.Source, h.Target)).ToArray(),
        VirtualDevice = session.Current.VirtualDevice with { DeviceId = checked((uint)DeviceId) }, Hiding = new(true, Routing == RoutingStrategy.CombinedVirtual)
    });
}
