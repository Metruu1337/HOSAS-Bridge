using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HOSASBridge.Core;

namespace HOSASBridge.Profiles;

public sealed class ProfileRepository(IBridgeLog? log = null) : IProfileRepository
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }, MaxDepth = 32
    };
    public static Profile Parse(string json)
    {
        if (json.Length > 2_000_000) throw new InvalidDataException("Profile exceeds the size limit.");
        using (var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 }))
        {
            static void CheckKeys(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var property in element.EnumerateObject())
                    { if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property."); CheckKeys(property.Value); }
                }
                else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) CheckKeys(item);
            }
            CheckKeys(document.RootElement);
        }
        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { MaxDepth = 32 }) as JsonObject ?? throw new InvalidDataException("Profile must be a JSON object.");
        var version = root["schemaVersion"]?.GetValue<int>() ?? 0;
        if (version == 0)
        {
            // The legacy schema used name rather than displayName and implicit Hybrid routing.
            root["displayName"] ??= root["name"]?.DeepClone() ?? JsonValue.Create("Imported profile");
            root.Remove("name"); root["routing"] ??= "Hybrid"; root["schemaVersion"] = 1;
        }
        if (root["schemaVersion"]?.GetValue<int>() == 1) root["schemaVersion"] = 2;
        var profile = root.Deserialize<Profile>(JsonOptions) ?? throw new InvalidDataException("Empty profile.");
        ProfileValidator.EnsureValid(profile); return profile;
    }
    public Profile Load(string path)
    {
        if (!File.Exists(path)) return ProfileDefaults.Wardogs();
        try { return WithLocalBindings(path, Parse(File.ReadAllText(path))); }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or InvalidOperationException or FormatException)
        {
            log?.Write("Warning", "profile.corrupt", "Recovering profile from backup.", ex);
            if (File.Exists(path + ".bak"))
            {
                try { return WithLocalBindings(path, Parse(File.ReadAllText(path + ".bak"))); }
                catch (Exception backup) when (backup is JsonException or InvalidDataException or InvalidOperationException or FormatException)
                { log?.Write("Error", "profile.backup.corrupt", "Using built-in profile; original files retained.", backup); }
            }
            return ProfileDefaults.Wardogs();
        }
    }
    public void Save(string path, Profile profile)
    {
        ProfileValidator.EnsureValid(profile);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var bindings = path + ".bindings.json";
        File.WriteAllText(bindings + ".tmp", JsonSerializer.Serialize(profile.Devices, JsonOptions));
        File.Move(bindings + ".tmp", bindings, true);
        profile = profile with { Devices = [] };
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(profile, JsonOptions));
        if (File.Exists(path))
        {
            // Only valid data may become the last-known-good backup.
            bool valid;
            try { _ = Parse(File.ReadAllText(path)); valid = true; }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or InvalidOperationException or FormatException) { valid = false; }
            if (valid) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path, true);
        }
        else { File.Move(temp, path); File.Copy(path, path + ".bak"); }
    }
    public static Profile LoadStrict(string path) => WithLocalBindings(path, Parse(File.ReadAllText(path)));
    private static Profile WithLocalBindings(string path, Profile profile)
    {
        var bindings = path + ".bindings.json";
        if (!File.Exists(bindings)) return profile; // Schema 0/1 migration retains original identities until the next save.
        var devices = JsonSerializer.Deserialize<Dictionary<DeviceRole, DeviceIdentity>>(File.ReadAllText(bindings), JsonOptions)
            ?? throw new InvalidDataException("Invalid local device bindings.");
        var result = profile with { Devices = devices };
        ProfileValidator.EnsureValid(result);
        return result;
    }
    public static void Export(string path, Profile profile)
    {
        ProfileValidator.EnsureValid(profile);
        File.WriteAllText(path, JsonSerializer.Serialize(profile with { Devices = [], ProcessName = null }, JsonOptions));
    }
}
