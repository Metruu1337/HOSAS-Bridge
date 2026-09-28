using HOSASBridge.Core;
using HOSASBridge.Profiles;
namespace HOSASBridge.Core.Tests;
public sealed class MultiRoleTests
{
    [Fact] public void PedalsRouteWithoutTreatingThemAsLeftStick()
    {
        var p = ProfileDefaults.Wardogs() with { Axes = [new(DeviceRole.Pedals, PhysicalAxis.Rx, VirtualAxis.Ry, new() { CenterDeadzone = 0 })], Buttons = [], Povs = [], RoleRoutes = new() { [DeviceRole.Pedals] = new(false, true, true) } };
        var state = new PhysicalState { Connected = true }; state.Axes[(int)PhysicalAxis.Rx] = 65535;
        var output = new VirtualState(); new InputPipeline(p).Process(new Dictionary<DeviceRole, PhysicalState> { [DeviceRole.Pedals] = state }, output);
        Assert.Equal(1, output.Axes[(int)VirtualAxis.Ry]); Assert.Equal(0, output.Axes[0]);
    }
    [Fact] public void NativeRoleCannotLeakMappedInput()
    {
        var p = ProfileDefaults.Wardogs(RoutingStrategy.CombinedVirtual) with { RoleRoutes = new() { [DeviceRole.Left] = new(true, false, false) } };
        var left = new PhysicalState { Connected = true }; Array.Fill(left.Axes, 65535); Array.Fill(left.Buttons, true); left.Povs[0] = 9000;
        var result = new VirtualState(); new InputPipeline(p).Process(new PhysicalState(), left, result);
        Assert.All(result.Axes, a => Assert.Equal(0, a)); Assert.All(result.Buttons, b => Assert.False(b)); Assert.All(result.Povs, h => Assert.Equal(-1, h));
    }
    [Fact] public void ModeOverridesInheritTransformsAndButtons()
    {
        var p = ProfileDefaults.Wardogs() with { Modes = [new("BASE", null, []) { AxisOverrides = new() { [VirtualAxis.X] = new() { Sensitivity = .5, CenterDeadzone = 0 } }, ButtonOverrides = [new(DeviceRole.Right, 2, 3)] }, new("CHILD", "BASE", [VirtualAxis.Y])] };
        var state = new PhysicalState { Connected = true }; Array.Fill(state.Axes, 65535); state.Buttons[1] = true;
        var pipe = new InputPipeline(p); pipe.SelectMode("CHILD"); var output = new VirtualState(); pipe.Process(state, new(), output);
        Assert.Equal(.5, output.Axes[0]); Assert.Equal(-1, output.Axes[1]); Assert.True(output.Buttons[2]); Assert.False(output.Buttons[1]);
    }
    [Fact] public void DisconnectReleasesPedalButtonsAndPov()
    {
        var p = ProfileDefaults.Wardogs(RoutingStrategy.CombinedVirtual) with { Axes = [], Buttons = [new(DeviceRole.Pedals, 1, 1)], Povs = [new(DeviceRole.Pedals, 1, 1)] };
        var state = new PhysicalState { Connected = true }; state.Buttons[0] = true; state.Povs[0] = 9000;
        var states = new Dictionary<DeviceRole, PhysicalState> { [DeviceRole.Pedals] = state }; var output = new VirtualState(); var pipe = new InputPipeline(p);
        pipe.Process(states, output); Assert.True(output.Buttons[0]); state.Connected = false; pipe.Process(states, output);
        Assert.False(output.Buttons[0]); Assert.Equal(-1, output.Povs[0]);
    }
}
