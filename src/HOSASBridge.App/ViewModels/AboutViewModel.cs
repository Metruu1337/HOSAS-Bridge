using System.Diagnostics;
using HOSASBridge.Infrastructure;
namespace HOSASBridge.App.ViewModels;
public sealed class AboutViewModel
{
    public string Build => BuildInfo.Summary;
    public string Privacy => L.T("Open source · Local processing\nNo telemetry · No gameplay automation\nNo game memory access · No keyboard logging");
    public string Signing => L.T("Public beta. Application binaries are unsigned unless a release explicitly states otherwise.");
    public AsyncCommand Source { get; }
    public AsyncCommand Security { get; }
    public AsyncCommand Licenses { get; }
    public AboutViewModel(Action<Exception> error)
    {
        Source = new(() => { if (!Uri.TryCreate(BuildInfo.Source, UriKind.Absolute, out var uri) || uri.Scheme != "https") throw new InvalidOperationException("Source repository URL is not configured in this local build."); Open(uri.AbsoluteUri); return Task.CompletedTask; }, error);
        Security = new(() => { Open(Path.Combine(AppContext.BaseDirectory, "docs", L.T("SECURITY_AND_TRUST.md"))); return Task.CompletedTask; }, error);
        Licenses = new(() => { Open(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md")); return Task.CompletedTask; }, error);
    }
    private static void Open(string target) => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
}
