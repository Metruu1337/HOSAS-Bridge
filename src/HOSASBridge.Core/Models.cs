namespace HOSASBridge.Core;

public enum DeviceRole { Right, Left, Throttle, Pedals, Other }
public enum RoutingStrategy { Hybrid, CombinedVirtual }
// Preserve the first four numeric values used by legacy profiles.
public enum PhysicalAxis { X, Y, Twist, Throttle, Z, Rx, Ry, Slider1 }
public enum VirtualAxis { X, Y, Z, Rx, Ry, Rz, Slider0, Slider1 }
public enum CurveKind { Linear, Exponential }
public enum BindingBehavior { Toggle, Hold }

public sealed record DeviceIdentity(Guid InstanceGuid, string InstanceId, string InterfacePath,
    Guid ContainerId, string Location, string ProductName, int VendorId, int ProductId)
{
    public string Manufacturer { get; init; } = "";
    public int AxisCount { get; init; }
    public int ButtonCount { get; init; }
    public int PovCount { get; init; }
    public string AvailableAxes { get; init; } = "";
}

public sealed record Calibration(double Min = 0, double Center = 32767.5, double Max = 65535);
public sealed record AxisTransform
{
    public Calibration Calibration { get; init; } = new();
    public bool Invert { get; init; }
    public double CenterDeadzone { get; init; } = 0.03;
    public double OuterDeadzone { get; init; }
    public double Sensitivity { get; init; } = 1;
    public CurveKind Curve { get; init; }
    public double Exponent { get; init; } = 1.5;
    public double Saturation { get; init; } = 1;
}
public sealed record AxisMapping(DeviceRole Role, PhysicalAxis Source, VirtualAxis Target, AxisTransform Transform)
{
    public string Meaning { get; init; } = "";
}
public sealed record ButtonMapping(DeviceRole Role, int Source, int Target);
public sealed record PovMapping(DeviceRole Role, int Source, int Target);
public sealed record ModeDefinition(string Id, string? Parent, VirtualAxis[] InvertAxes)
{
    public Dictionary<VirtualAxis, AxisTransform> AxisOverrides { get; init; } = [];
    public ButtonMapping[]? ButtonOverrides { get; init; }
}
public sealed record ModeBinding(DeviceRole Role, int Button, string BaseMode, string ActiveMode, BindingBehavior Behavior);
public sealed record VirtualRequirements(uint DeviceId = 1, int Buttons = 32, int Povs = 2);
public sealed record HidingPolicy(bool HideRight = true, bool HideLeft = false);
public sealed record RoleRouting(bool Native, bool Hidden, bool Virtualized);
public sealed record Profile
{
    public int SchemaVersion { get; init; } = 2;
    public string ProfileId { get; init; } = "wardogs";
    public string DisplayName { get; init; } = "WARDOGS";
    public string? ProcessName { get; init; }
    public RoutingStrategy Routing { get; init; }
    public Dictionary<DeviceRole, DeviceIdentity> Devices { get; init; } = [];
    public AxisMapping[] Axes { get; init; } = [];
    public ButtonMapping[] Buttons { get; init; } = [];
    public PovMapping[] Povs { get; init; } = [];
    public ModeDefinition[] Modes { get; init; } = [];
    public ModeBinding? ModeBinding { get; init; }
    public VirtualRequirements VirtualDevice { get; init; } = new();
    public HidingPolicy Hiding { get; init; } = new();
    public Dictionary<DeviceRole, RoleRouting> RoleRoutes { get; init; } = [];
    public RoleRouting Route(DeviceRole role) => RoleRoutes.TryGetValue(role, out var route) ? route
        : new(role == DeviceRole.Left && Routing == RoutingStrategy.Hybrid,
            role == DeviceRole.Right ? Hiding.HideRight : role == DeviceRole.Left && Hiding.HideLeft,
            role == DeviceRole.Right || Routing == RoutingStrategy.CombinedVirtual);
}

// Buffers are owned by the input worker. UI readers receive copies at a lower rate.
public sealed class PhysicalState
{
    public bool Connected { get; set; }
    public double[] Axes { get; } = new double[8];
    public bool[] Buttons { get; } = new bool[128];
    public int[] Povs { get; } = [-1, -1, -1, -1];
    public void Clear() { Connected = false; Array.Fill(Axes, 32767.5); Array.Clear(Buttons); Array.Fill(Povs, -1); }
    public PhysicalState Clone()
    {
        var copy = new PhysicalState { Connected = Connected };
        Axes.CopyTo(copy.Axes, 0); Buttons.CopyTo(copy.Buttons, 0); Povs.CopyTo(copy.Povs, 0); return copy;
    }
}
public sealed class VirtualState
{
    public double[] Axes { get; } = new double[8];
    public bool[] Buttons { get; } = new bool[128];
    public int[] Povs { get; } = [-1, -1, -1, -1];
    public void Clear() { Array.Clear(Axes); Array.Clear(Buttons); Array.Fill(Povs, -1); }
    public VirtualState Clone()
    {
        var copy = new VirtualState(); Axes.CopyTo(copy.Axes, 0); Buttons.CopyTo(copy.Buttons, 0); Povs.CopyTo(copy.Povs, 0); return copy;
    }
}
public sealed record Health(bool Ready, string Detail);
public sealed record HidingHealth(bool Installed, bool Whitelisted, bool RightHidden, bool LeftHidden, bool Active, string Detail, bool VirtualVisible = true)
{
    public bool PolicyMatches { get; init; } = true;
}
public sealed record AxisSample(DeviceRole Role, PhysicalAxis Source, double Raw, double Normalized, double Transformed, double? Output);
public sealed record PipelineSnapshot(DateTimeOffset Time, PhysicalState Right, PhysicalState Left, VirtualState Output,
    string Mode, bool Running, string Status, double InputHz, double OutputHz, double ProcessingMs)
{
    public IReadOnlyDictionary<DeviceRole, PhysicalState> Roles { get; init; } = new Dictionary<DeviceRole, PhysicalState>();
}
