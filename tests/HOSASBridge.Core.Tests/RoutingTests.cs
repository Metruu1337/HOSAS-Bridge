using HOSASBridge.Core;
using HOSASBridge.Profiles;

namespace HOSASBridge.Core.Tests;

public sealed class RoutingTests
{
    private static Profile Profile(RoutingStrategy routing = RoutingStrategy.Hybrid)
    {
        var p = ProfileDefaults.Wardogs(routing);
        return p with { Axes = p.Axes.Select(a => a with { Transform = new AxisTransform { Calibration = new(-1, 0, 1), CenterDeadzone = 0 } }).ToArray() };
    }
    private static PhysicalState Stick()
    { var s = new PhysicalState { Connected = true }; s.Axes[0] = 0.3; s.Axes[1] = 0.7; s.Axes[2] = -0.4; s.Buttons[0] = true; s.Povs[0] = 9000; return s; }
    [Theory][InlineData(false)][InlineData(true)]
    public void MinigunOnlyReversesPitchWithBaseXor(bool baseInvert)
    {
        var p = Profile(); p = p with { Axes = p.Axes.Select(a => a.Target == VirtualAxis.Y ? a with { Transform = a.Transform with { Invert = baseInvert } } : a).ToArray() };
        var pipeline = new InputPipeline(p); var state = new VirtualState(); var stick = Stick();
        pipeline.Process(stick, Stick(), state); var before = state.Axes.ToArray();
        Assert.Equal(baseInvert ? -0.7 : 0.7, before[1], 10);
        pipeline.SelectMode("MINIGUN"); pipeline.Process(stick, Stick(), state);
        Assert.Equal(-before[1], state.Axes[1]); Assert.Equal(before[0], state.Axes[0]); Assert.Equal(before[2], state.Axes[2]);
    }
    [Fact] public void HybridNeverDuplicatesLeft()
    {
        var pipeline = new InputPipeline(Profile()); var output = new VirtualState();
        pipeline.Process(new(), Stick(), output);
        Assert.All(output.Axes, a => Assert.Equal(0, a)); Assert.All(output.Buttons, Assert.False); Assert.All(output.Povs, h => Assert.Equal(-1, h));
    }
    [Fact] public void CombinedRoutesBoth()
    {
        var pipeline = new InputPipeline(Profile(RoutingStrategy.CombinedVirtual)); var output = new VirtualState();
        pipeline.Process(Stick(), Stick(), output);
        Assert.Equal(0.7, output.Axes[(int)VirtualAxis.Slider0]); Assert.True(output.Buttons[16]); Assert.Equal(9000, output.Povs[1]);
    }
    [Fact] public void DisconnectClearsAxesButtonsHatsAndReconnectRestores()
    {
        var pipeline = new InputPipeline(Profile()); var output = new VirtualState(); var right = Stick();
        pipeline.Process(right, new(), output); Assert.True(output.Buttons[0]);
        right.Connected = false; pipeline.Process(right, new(), output);
        Assert.All(output.Axes, a => Assert.Equal(0, a)); Assert.All(output.Buttons, Assert.False); Assert.All(output.Povs, h => Assert.Equal(-1, h));
        right.Connected = true; pipeline.Process(right, new(), output); Assert.Equal(0.7, output.Axes[1]); Assert.True(output.Buttons[0]);
    }
    [Fact] public void ButtonReleaseIsNotSticky()
    {
        var pipeline = new InputPipeline(Profile()); var output = new VirtualState(); var right = Stick();
        pipeline.Process(right, new(), output); right.Buttons[0] = false; pipeline.Process(right, new(), output); Assert.False(output.Buttons[0]);
    }
    [Fact] public void ToggleIsRisingEdgeAndConsumesBoundButton()
    {
        var p = Profile() with { ModeBinding = new(DeviceRole.Right, 1, "NORMAL", "MINIGUN", BindingBehavior.Toggle) };
        var pipeline = new InputPipeline(p); var output = new VirtualState(); var right = Stick();
        pipeline.Process(right, new(), output); Assert.Equal("MINIGUN", pipeline.Mode); Assert.False(output.Buttons[0]);
        pipeline.Process(right, new(), output); Assert.Equal("MINIGUN", pipeline.Mode);
        right.Buttons[0] = false; pipeline.Process(right, new(), output); right.Buttons[0] = true; pipeline.Process(right, new(), output); Assert.Equal("NORMAL", pipeline.Mode);
    }
    [Fact] public void HoldRestoresOnDisconnect()
    {
        var p = Profile() with { ModeBinding = new(DeviceRole.Right, 1, "NORMAL", "MINIGUN", BindingBehavior.Hold) };
        var pipeline = new InputPipeline(p); var output = new VirtualState(); var right = Stick();
        pipeline.Process(right, new(), output); Assert.Equal("MINIGUN", pipeline.Mode);
        right.Connected = false; pipeline.Process(right, new(), output); Assert.Equal("NORMAL", pipeline.Mode);
    }
    [Fact] public void ModeInheritanceComposesInversions()
    {
        var p = Profile() with { Modes = [new("A", null, [VirtualAxis.Y]), new("B", "A", [VirtualAxis.Y, VirtualAxis.X])] };
        var modes = new ModeController(p); modes.Select("B"); Assert.False(modes.Inverts(VirtualAxis.Y)); Assert.True(modes.Inverts(VirtualAxis.X)); Assert.False(modes.Inverts(VirtualAxis.Z));
    }
    [Fact] public void UnknownModeRejected() => Assert.Throws<ArgumentException>(() => new ModeController(Profile()).Select("missing"));
}
