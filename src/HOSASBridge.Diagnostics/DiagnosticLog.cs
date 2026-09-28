using System.IO.Compression;
using System.Text.Json;
using HOSASBridge.Core;

namespace HOSASBridge.Diagnostics;

public sealed class DiagnosticLog : IBridgeLog
{
    private readonly object gate = new();
    private readonly string directory;
    public string MinimumLevel { get; set; } = "Information";
    public DiagnosticLog(string directory) { this.directory = directory; Directory.CreateDirectory(directory); }
    private static int Rank(string level) => level switch { "Debug" => 0, "Information" => 1, "Warning" => 2, _ => 3 };
    public void Write(string level, string eventName, string message, Exception? exception = null)
    {
        if (Rank(level) < Rank(MinimumLevel)) return;
        lock (gate)
        {
            try
            {
                var path = Path.Combine(directory, $"bridge-{DateTime.UtcNow:yyyyMMdd}.jsonl");
                if (File.Exists(path) && new FileInfo(path).Length > 5_000_000) File.Move(path, path + ".1", true);
                File.AppendAllText(path, JsonSerializer.Serialize(new { time = DateTimeOffset.UtcNow, level, eventName, message, error = exception?.ToString() }) + Environment.NewLine);
                foreach (var old in Directory.EnumerateFiles(directory).Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-14))) File.Delete(old);
            }
            catch (IOException ex) { System.Diagnostics.Trace.WriteLine("HOSAS log unavailable: " + ex.Message); }
            catch (UnauthorizedAccessException ex) { System.Diagnostics.Trace.WriteLine("HOSAS log denied: " + ex.Message); }
        }
    }
    public void Export(string target, object sanitizedSettings, object profile, object devices, object dependencies)
    {
        lock (gate)
        {
            using var zip = ZipFile.Open(target, ZipArchiveMode.Create);
            string Sanitize(string text)
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (home.Length > 0) text = text.Replace(home.Replace("\\", "\\\\"), "%USERPROFILE%", StringComparison.OrdinalIgnoreCase).Replace(home, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
                return text;
            }
            foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl*"))
            { using var writer = new StreamWriter(zip.CreateEntry("Logs/" + Path.GetFileName(path)).Open()); writer.Write(Sanitize(File.ReadAllText(path))); }
            void Add(string name, object data)
            { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write(Sanitize(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }))); }
            Add("settings.json", sanitizedSettings); Add("profile.json", profile); Add("devices.json", devices); Add("dependencies.json", dependencies);
        }
    }
}
