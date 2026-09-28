using System.Text.Json;
using HOSASBridge.Core;

namespace HOSASBridge.Profiles;

public sealed record ControllerPreset(string Id, string Name, int VendorId, int ProductId, Dictionary<PhysicalAxis, string> AxisLabels);
public static class PresetCatalog
{
    public static IReadOnlyList<ControllerPreset> Controllers { get; } = LoadControllers();
    public static string[] Names => ["WARDOGS + Dual T.16000M", "Generic HOSAS", "Custom"];
    private static ControllerPreset[] LoadControllers()
    {
        using var stream = typeof(PresetCatalog).Assembly.GetManifestResourceStream("HOSASBridge.controllers.json")!;
        return JsonSerializer.Deserialize<ControllerPreset[]>(stream, ProfileRepository.JsonOptions)!;
    }
    public static string AxisLabel(DeviceIdentity? device, PhysicalAxis axis)
        => Controllers.FirstOrDefault(p => p.VendorId == device?.VendorId && p.ProductId == device?.ProductId)?.AxisLabels.GetValueOrDefault(axis)
            ?? axis switch { PhysicalAxis.X => "Horizontal", PhysicalAxis.Y => "Vertical", PhysicalAxis.Twist => "Rotation Z", PhysicalAxis.Throttle => "Slider 0", _ => axis.ToString() };
    public static Profile Create(string name)
    {
        if (name == Names[0]) return ProfileDefaults.Wardogs();
        var p = ProfileDefaults.Wardogs(RoutingStrategy.CombinedVirtual);
        return p with { ProfileId = name == "Custom" ? Guid.NewGuid().ToString("N") : "generic-hosas", DisplayName = name, Modes = [new("NORMAL", null, [])] };
    }
}
