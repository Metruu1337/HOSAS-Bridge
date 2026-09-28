using System.Text.Json;
using HOSASBridge.Core;
using HOSASBridge.Profiles;

namespace HOSASBridge.Profiles.Tests;

public sealed class ProfileTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "HosasTests-" + Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(directory, "profile.json");
    private static string Json(Profile p) => JsonSerializer.Serialize(p, ProfileRepository.JsonOptions);
    [Fact] public void BuiltinValid() => Assert.Empty(ProfileValidator.Validate(ProfileDefaults.Wardogs()));
    [Fact] public void CombinedValid() => Assert.Empty(ProfileValidator.Validate(ProfileDefaults.Wardogs(RoutingStrategy.CombinedVirtual)));
    [Fact] public void JsonRoundTrip() { var p = ProfileRepository.Parse(Json(ProfileDefaults.Wardogs())); Assert.Equal("WARDOGS", p.DisplayName); Assert.Equal(8, p.Axes.Length); Assert.Equal(32, p.Buttons.Length); }
    [Fact] public void FutureVersionRejected() => Assert.Throws<InvalidDataException>(() => ProfileRepository.Parse(Json(ProfileDefaults.Wardogs() with { SchemaVersion = 9 })));
    [Fact] public void LegacyMigrated()
    {
        var json = Json(ProfileDefaults.Wardogs()).Replace("\"schemaVersion\": 2", "\"schemaVersion\": 0").Replace("\"displayName\"", "\"name\"");
        var p = ProfileRepository.Parse(json); Assert.Equal(2, p.SchemaVersion); Assert.Equal("WARDOGS", p.DisplayName);
    }
    [Fact] public void NoArbitraryTypeDeserialization() { var json = Json(ProfileDefaults.Wardogs()).Insert(1, "\"$type\":\"System.Diagnostics.Process\","); Assert.Equal("wardogs", ProfileRepository.Parse(json).ProfileId); }
    [Fact] public void MissingModesRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Modes = [] }));
    [Fact] public void CycleRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Modes = [new("A", "B", []), new("B", "A", [])] }));
    [Fact] public void MissingParentRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Modes = [new("A", "unknown", [])] }));
    [Fact] public void DuplicateModeRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Modes = [new("A", null, []), new("A", null, [])] }));
    [Theory][InlineData(-0.1, 0)][InlineData(0.5, 0.5)][InlineData(0, -0.1)]
    public void InvalidDeadzoneRejected(double center, double outer)
    { var p = ProfileDefaults.Wardogs(); p.Axes[0] = p.Axes[0] with { Transform = new() { CenterDeadzone = center, OuterDeadzone = outer } }; Assert.NotEmpty(ProfileValidator.Validate(p)); }
    [Fact] public void InvalidCalibrationRejected() { var p = ProfileDefaults.Wardogs(); p.Axes[0] = p.Axes[0] with { Transform = new() { Calibration = new(10, 5, 20) } }; Assert.NotEmpty(ProfileValidator.Validate(p)); }
    [Fact] public void InvalidButtonRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Buttons = [new(DeviceRole.Right, 0, 999)] }));
    [Fact] public void InvalidPovRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Povs = [new(DeviceRole.Right, 1, 9)] }));
    [Fact] public void AxisConflictRejected() { var p = ProfileDefaults.Wardogs(); p.Axes[1] = p.Axes[1] with { Target = VirtualAxis.X }; Assert.NotEmpty(ProfileValidator.Validate(p)); }
    [Fact] public void HiddenNativeLeftRejected() => Assert.NotEmpty(ProfileValidator.Validate(ProfileDefaults.Wardogs() with { Hiding = new(true, true) }));
    [Fact] public void NullCollectionsRejected() => Assert.Throws<InvalidDataException>(() => ProfileRepository.Parse("{\"schemaVersion\":1,\"axes\":null}"));
    [Fact] public void SaveLoadAndBackup()
    {
        var repo = new ProfileRepository(); repo.Save(PathName, ProfileDefaults.Wardogs()); repo.Save(PathName, ProfileDefaults.Wardogs() with { DisplayName = "Second" });
        Assert.Equal("Second", repo.Load(PathName).DisplayName); Assert.Equal("WARDOGS", ProfileRepository.Parse(File.ReadAllText(PathName + ".bak")).DisplayName);
    }
    [Fact] public void CorruptProfileRecoversBackup()
    { var repo = new ProfileRepository(); repo.Save(PathName, ProfileDefaults.Wardogs() with { DisplayName = "Known good" }); File.WriteAllText(PathName, "broken"); Assert.Equal("Known good", repo.Load(PathName).DisplayName); }
    [Fact] public void CorruptBothRecoversBuiltinWithoutDeletingEvidence()
    { Directory.CreateDirectory(directory); File.WriteAllText(PathName, "broken"); File.WriteAllText(PathName + ".bak", "also broken"); Assert.Equal("WARDOGS", new ProfileRepository().Load(PathName).DisplayName); Assert.Equal("broken", File.ReadAllText(PathName)); }
    [Fact] public void InvalidSaveCannotOverwriteGood()
    { var repo = new ProfileRepository(); repo.Save(PathName, ProfileDefaults.Wardogs()); Assert.Throws<InvalidDataException>(() => repo.Save(PathName, ProfileDefaults.Wardogs() with { Modes = [] })); Assert.Equal("WARDOGS", repo.Load(PathName).DisplayName); }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
