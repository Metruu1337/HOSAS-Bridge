using System.Reflection;
namespace HOSASBridge.Infrastructure;
public static class BuildInfo
{
    private static Assembly Assembly => typeof(BuildInfo).Assembly;
    public static string Version => Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
    public static string Commit => Metadata("Commit");
    public static string Tag => Metadata("Tag");
    public static string Source => Metadata("Source");
    public static string Summary => $"HOSAS Bridge {Version}\nCommit: {Commit}\nTag: {Tag}\nRelease / win-x64";
    private static string Metadata(string name) => Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == name)?.Value ?? "unavailable";
}
