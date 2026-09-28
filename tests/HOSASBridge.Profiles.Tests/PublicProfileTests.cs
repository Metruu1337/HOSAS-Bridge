using HOSASBridge.Core;
using HOSASBridge.Profiles;
using System.Text.Json;

namespace HOSASBridge.Profiles.Tests;
public sealed class PublicProfileTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "HosasPublic-" + Guid.NewGuid());
    private static DeviceIdentity Device => new(Guid.NewGuid(), "HID\\PRIVATE-SERIAL", "private-path", Guid.NewGuid(), "private-port", "Stick", 1, 2);
    [Fact] public void ExportStripsIdentityAndMachineProcessAssociation()
    {
        Directory.CreateDirectory(root); var path = Path.Combine(root, "export.json");
        ProfileRepository.Export(path, ProfileDefaults.Wardogs() with { Devices = new() { [DeviceRole.Right] = Device }, ProcessName = "local-game" });
        var text = File.ReadAllText(path); Assert.DoesNotContain("PRIVATE", text); Assert.DoesNotContain("local-game", text);
        Assert.Empty(ProfileRepository.Parse(text).Devices);
    }
    [Fact] public void LocalBindingsRoundTripSeparately()
    {
        var path = Path.Combine(root, "profile.json"); var d = Device; var repo = new ProfileRepository();
        repo.Save(path, ProfileDefaults.Wardogs() with { Devices = new() { [DeviceRole.Right] = d } });
        Assert.DoesNotContain(d.InstanceId.Replace("\\", "\\\\"), File.ReadAllText(path)); Assert.Equal(d, repo.Load(path).Devices[DeviceRole.Right]);
    }
    [Fact] public void LegacyIdentitySurvivesMigrationAndSave()
    {
        var path = Path.Combine(root, "profile.json"); Directory.CreateDirectory(root); var d = Device;
        File.WriteAllText(path, JsonSerializer.Serialize(ProfileDefaults.Wardogs() with { SchemaVersion = 1, Devices = new() { [DeviceRole.Right] = d } }, ProfileRepository.JsonOptions));
        var repo = new ProfileRepository(); var migrated = repo.Load(path); repo.Save(path, migrated);
        Assert.Equal(2, migrated.SchemaVersion); Assert.Equal(d, repo.Load(path).Devices[DeviceRole.Right]);
    }
    [Fact] public void DuplicateJsonKeysRejected() => Assert.Throws<InvalidDataException>(() => ProfileRepository.Parse("{\"schemaVersion\":2,\"schemaVersion\":1}"));
    [Fact] public void BuiltinCustomizationDoesNotMutateOriginal()
    { var first = PresetCatalog.Create(PresetCatalog.Names[0]); first.Axes[0] = first.Axes[0] with { Transform = new() { Invert = true } }; Assert.False(PresetCatalog.Create(PresetCatalog.Names[0]).Axes[0].Transform.Invert); }
    [Fact] public void GenericPresetHasNoGameSpecificMode() => Assert.Single(PresetCatalog.Create("Generic HOSAS").Modes);
    [Fact] public void InvalidModeTransformRejected()
    { var p = ProfileDefaults.Wardogs(); p.Modes[1] = p.Modes[1] with { AxisOverrides = new() { [VirtualAxis.Y] = new() { Sensitivity = -1 } } }; Assert.NotEmpty(ProfileValidator.Validate(p)); }
    [Fact] public void UnknownRoleRejected()
    { var p = ProfileDefaults.Wardogs() with { Devices = new() { [(DeviceRole)999] = Device } }; Assert.NotEmpty(ProfileValidator.Validate(p)); }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
