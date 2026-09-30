using System.Text.Json;

namespace HOSASBridge.Infrastructure;

public sealed record UserSettings
{
    public string Language { get; init; } = "en";
    public bool StartBridgeAutomatically { get; init; }
    public bool StartMinimized { get; init; }
    public bool CloseToTray { get; init; } = true;
    public bool AudioCues { get; init; } = true;
    public bool SetupCompleted { get; init; }
    public string LogLevel { get; init; } = "Information";
    public bool AutoActivateOnProcess { get; init; }
}
public static class AppPaths
{
    public static string Data => Environment.GetEnvironmentVariable("HOSASBRIDGE_DATA") is { Length: > 0 } path
        ? Path.GetFullPath(path) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HOSASBridge");
    public static string Profile => Path.Combine(Data, "profile.json");
    public static string Settings => Path.Combine(Data, "settings.json");
    public static string Logs => Path.Combine(Data, "Logs");
}
public sealed class SettingsStore
{
    public UserSettings Load()
    {
        foreach (var path in new[] { AppPaths.Settings, AppPaths.Settings + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));
                if (settings is not null && settings.Language is "pl" or "en" && settings.LogLevel is "Debug" or "Information" or "Warning" or "Error") return settings;
            }
            catch (JsonException) { /* The backup is attempted; invalid files are preserved for diagnosis. */ }
        }
        return new();
    }
    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(AppPaths.Data);
        var path = AppPaths.Settings; File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(path))
        {
            bool valid;
            try { valid = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path)) is { Language: "pl" or "en" }; }
            catch (JsonException) { valid = false; }
            if (valid) File.Replace(path + ".tmp", path, path + ".bak"); else File.Move(path + ".tmp", path, true);
        }
        else File.Move(path + ".tmp", path);
    }
}
