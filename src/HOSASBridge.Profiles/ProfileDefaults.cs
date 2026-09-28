using System.Text.Json;
using HOSASBridge.Core;
namespace HOSASBridge.Profiles;
public static class ProfileDefaults
{
    public static Profile Wardogs(RoutingStrategy routing = RoutingStrategy.Hybrid)
    {
        using var stream = typeof(ProfileDefaults).Assembly.GetManifestResourceStream("HOSASBridge.wardogs.json")!;
        var profile = JsonSerializer.Deserialize<Profile>(stream, ProfileRepository.JsonOptions) ?? throw new InvalidDataException("Missing built-in preset.");
        return profile with { Routing = routing, Hiding = new(true, routing == RoutingStrategy.CombinedVirtual) };
    }
}
