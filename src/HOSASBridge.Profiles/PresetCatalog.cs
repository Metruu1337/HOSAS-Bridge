using System.Text.Json;
using HOSASBridge.Core;

namespace HOSASBridge.Profiles;

public sealed record ControllerPreset(string Id, string Name, int VendorId, int ProductId, Dictionary<PhysicalAxis, string> AxisLabels);
public static class PresetCatalog
{
    public static IReadOnlyList<ControllerPreset> Controllers { get; } = LoadControllers();
    public static string[] Names => ["WARDOGS + Dual T.16000M", "Generic HOSAS", "Custom", "Xbox gamepad", "PlayStation gamepad"];
    public static bool IsGamepad(Profile profile) => profile.ProfileId is "xbox-gamepad" or "playstation-gamepad";
    private static ControllerPreset[] LoadControllers()
    {
        using var stream = typeof(PresetCatalog).Assembly.GetManifestResourceStream("HOSASBridge.controllers.json")!;
        return JsonSerializer.Deserialize<ControllerPreset[]>(stream, ProfileRepository.JsonOptions)!;
    }
    public static string AxisLabel(DeviceIdentity? device, PhysicalAxis axis)
        => (device?.Backend == "XInput" || device?.VendorId == 0x054c ? axis switch
            { PhysicalAxis.X => "Left stick X", PhysicalAxis.Y => "Left stick Y", PhysicalAxis.Z => "Right stick X",
              PhysicalAxis.Twist => "Right stick Y", PhysicalAxis.Rx => "Left trigger", PhysicalAxis.Ry => "Right trigger", _ => null } : null)
            ?? Controllers.FirstOrDefault(p => p.VendorId == device?.VendorId && p.ProductId == device?.ProductId)?.AxisLabels.GetValueOrDefault(axis)
            ?? axis switch { PhysicalAxis.X => "Horizontal", PhysicalAxis.Y => "Vertical", PhysicalAxis.Twist => "Rotation Z", PhysicalAxis.Throttle => "Slider 0", _ => axis.ToString() };
    public static Profile Create(string name)
    {
        if (name is "Xbox gamepad" or "PlayStation gamepad") return Gamepad(name == "Xbox gamepad");
        if (name == Names[0]) return ProfileDefaults.Wardogs();
        var p = ProfileDefaults.Wardogs(RoutingStrategy.CombinedVirtual);
        return p with { ProfileId = name == "Custom" ? Guid.NewGuid().ToString("N") : "generic-hosas", DisplayName = name, Modes = [new("NORMAL", null, [])] };
    }
    private static Profile Gamepad(bool xbox) => new()
    {
        ProfileId = xbox ? "xbox-gamepad" : "playstation-gamepad", DisplayName = xbox ? "Xbox gamepad" : "PlayStation gamepad",
        Routing = RoutingStrategy.CombinedVirtual, Hiding = new(false, false),
        RoleRoutes = Enum.GetValues<DeviceRole>().ToDictionary(r => r, r => new RoleRouting(r == DeviceRole.Right, false, r == DeviceRole.Right)),
        Axes = new[] { (PhysicalAxis.X, VirtualAxis.X), (PhysicalAxis.Y, VirtualAxis.Y), (PhysicalAxis.Z, VirtualAxis.Rx),
            (PhysicalAxis.Twist, VirtualAxis.Ry), (PhysicalAxis.Rx, VirtualAxis.Z), (PhysicalAxis.Ry, VirtualAxis.Rz) }
            .Select(a => new AxisMapping(DeviceRole.Right, a.Item1, a.Item2,
                new AxisTransform { CenterDeadzone = a.Item1 is PhysicalAxis.Rx or PhysicalAxis.Ry ? 0 : 0.08 })).ToArray(),
        Buttons = Enumerable.Range(1, xbox ? 10 : 14).Select(b => new ButtonMapping(DeviceRole.Right, b, b)).ToArray(),
        Povs = [new(DeviceRole.Right, 1, 1)], Modes = [new("NORMAL", null, [])]
    };
}
