using HOSASBridge.Core;
using HOSASBridge.Input.DirectInput;
using HOSASBridge.Profiles;

namespace HOSASBridge.Integration.Tests;

public sealed class GamepadTests
{
    [Fact] public void SticksUseFullRangeAndExactNeutral()
    {
        var state = new PhysicalState();
        XInputProvider.Decode(new() { LeftX = short.MinValue, LeftY = short.MaxValue, RightX = short.MaxValue, RightY = short.MinValue }, state);
        Assert.Equal(0, state.Axes[(int)PhysicalAxis.X]); Assert.Equal(0, state.Axes[(int)PhysicalAxis.Y]);
        Assert.Equal(65535, state.Axes[(int)PhysicalAxis.Z]); Assert.Equal(65535, state.Axes[(int)PhysicalAxis.Twist]);
        XInputProvider.Decode(new(), state);
        foreach (var axis in new[] { PhysicalAxis.X, PhysicalAxis.Y, PhysicalAxis.Z, PhysicalAxis.Twist }) Assert.Equal(32767.5, state.Axes[(int)axis]);
    }
    [Theory]
    [InlineData(0, 0)] [InlineData(255, 0)] [InlineData(0, 255)] [InlineData(255, 255)] [InlineData(100, 200)]
    public void TriggersRemainIndependentThroughPipeline(byte left, byte right)
    {
        var input = new PhysicalState(); var output = new VirtualState();
        XInputProvider.Decode(new() { LeftTrigger = left, RightTrigger = right }, input);
        new InputPipeline(PresetCatalog.Create("Xbox gamepad")).Process(input, new PhysicalState(), output);
        Assert.Equal(left / 255d * 2 - 1, output.Axes[(int)VirtualAxis.Z], 10);
        Assert.Equal(right / 255d * 2 - 1, output.Axes[(int)VirtualAxis.Rz], 10);
    }
    [Theory]
    [InlineData(0, -1)] [InlineData(1, 0)] [InlineData(9, 4500)] [InlineData(8, 9000)]
    [InlineData(10, 13500)] [InlineData(2, 18000)] [InlineData(6, 22500)] [InlineData(4, 27000)]
    [InlineData(5, 31500)] [InlineData(15, -1)]
    public void DpadDecodesIncludingDiagonals(ushort buttons, int pov)
    {
        var state = new PhysicalState(); XInputProvider.Decode(new() { Buttons = buttons }, state);
        Assert.Equal(pov, state.Povs[0]); Assert.All(state.Buttons, b => Assert.False(b));
    }
    [Fact] public void ButtonsAreIndependentAndReleaseWithoutStaleData()
    {
        ushort[] masks = [0x1000, 0x2000, 0x4000, 0x8000, 0x100, 0x200, 0x20, 0x10, 0x40, 0x80];
        var state = new PhysicalState();
        for (var i = 0; i < masks.Length; i++)
        {
            XInputProvider.Decode(new() { Buttons = masks[i] }, state);
            Assert.True(state.Buttons[i]); Assert.Single(state.Buttons, b => b);
        }
        XInputProvider.Decode(new(), state); Assert.All(state.Buttons, b => Assert.False(b));
    }
    [Fact] public void ReconnectionRequiresIdentificationAndDisconnectNeutralizesOutput()
    {
        var api = new FakeApi(); using var provider = new XInputProvider(api);
        provider.Refresh(); var original = Assert.Single(provider.Discover()); var state = new PhysicalState();
        Assert.True(provider.Read(original, state)); Assert.True(state.Buttons[0]);
        provider.Refresh(); Assert.Equal(original, Assert.Single(provider.Discover()));
        api.Connected = false; Assert.False(provider.Read(original, state)); Assert.False(state.Connected);
        var output = new VirtualState(); new InputPipeline(PresetCatalog.Create("Xbox gamepad")).Process(state, new PhysicalState(), output);
        Assert.All(output.Axes, a => Assert.Equal(0, a)); Assert.All(output.Buttons, b => Assert.False(b));
        api.Connected = true; provider.Refresh(); var replacement = Assert.Single(provider.Discover());
        Assert.NotEqual(original.InstanceGuid, replacement.InstanceGuid);
        Assert.Null(new DeviceIdentityResolver().Resolve(original, provider.Discover()));
        Assert.False(provider.Read(original, state)); Assert.True(provider.Read(replacement, state));
    }
    [Theory] [InlineData("Xbox gamepad", 10)] [InlineData("PlayStation gamepad", 14)]
    public void PresetsAreValidSingleControllerMappings(string name, int buttons)
    {
        var profile = PresetCatalog.Create(name); ProfileValidator.EnsureValid(profile);
        Assert.Equal(6, profile.Axes.Length); Assert.Equal(buttons, profile.Buttons.Length);
        Assert.All(profile.Axes, axis => Assert.Equal(DeviceRole.Right, axis.Role));
        Assert.All(Enum.GetValues<DeviceRole>(), role => Assert.False(profile.Route(role).Hidden));
        Assert.Null(profile.ModeBinding);
        var roundtrip = ProfileRepository.Parse(System.Text.Json.JsonSerializer.Serialize(profile, ProfileRepository.JsonOptions));
        Assert.Equal(profile.Axes, roundtrip.Axes);
    }
    private sealed class FakeApi : IXInputApi
    {
        public bool Connected = true;
        public bool Read(uint slot, out XInputState state)
        { state = new() { Gamepad = new() { Buttons = 0x1000, LeftX = short.MaxValue, LeftTrigger = 255 } }; return Connected && slot == 0; }
    }
}
