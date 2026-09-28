using HOSASBridge.Infrastructure;
namespace HOSASBridge.Profiles.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string? previous = Environment.GetEnvironmentVariable("HOSASBRIDGE_DATA");
    private readonly string directory = Path.Combine(Path.GetTempPath(), "HosasSettings-" + Guid.NewGuid());
    public SettingsTests() { Environment.SetEnvironmentVariable("HOSASBRIDGE_DATA", directory); Directory.CreateDirectory(directory); }
    [Fact] public void CorruptSettingsUseBackupWithoutDeletingEvidence()
    {
        var store = new SettingsStore(); store.Save(new() { Language = "en" }); store.Save(new() { Language = "pl" });
        File.WriteAllText(AppPaths.Settings, "corrupt");
        Assert.Equal("en", store.Load().Language); Assert.Equal("corrupt", File.ReadAllText(AppPaths.Settings));
    }
    [Fact] public void SavingAfterCorruptionPreservesGoodBackup()
    {
        var store = new SettingsStore(); store.Save(new() { Language = "en" }); store.Save(new() { Language = "pl" });
        File.WriteAllText(AppPaths.Settings, "corrupt"); store.Save(new() { Language = "pl" });
        File.WriteAllText(AppPaths.Settings, "corrupt again"); Assert.Equal("en", store.Load().Language);
    }
    public void Dispose() { Environment.SetEnvironmentVariable("HOSASBRIDGE_DATA", previous); Directory.Delete(directory, true); }
}
