namespace HOSASBridge.Core;

public sealed class InputPipeline : IInputPipeline
{
    private readonly Profile profile;
    private readonly IAxisTransformer transformer;
    private readonly IModeController modes;
    private readonly Dictionary<DeviceRole, PhysicalState> pair = [];
    private IReadOnlyDictionary<DeviceRole, PhysicalState> currentStates = new Dictionary<DeviceRole, PhysicalState>();
    public InputPipeline(Profile profile, IAxisTransformer? transformer = null)
    { this.profile = profile; this.transformer = transformer ?? new AxisTransformer(); modes = new ModeController(profile); }
    public string Mode => modes.Current;
    public void SelectMode(string id) => modes.Select(id);
    public void Process(PhysicalState right, PhysicalState left, VirtualState output)
        { pair[DeviceRole.Right] = right; pair[DeviceRole.Left] = left; Process(pair, output); }
    public void Process(IReadOnlyDictionary<DeviceRole, PhysicalState> states, VirtualState output)
        { currentStates = states; ProcessRoleStates(output); }
    private PhysicalState? State(DeviceRole role) => currentStates.GetValueOrDefault(role);
    private void ProcessRoleStates(VirtualState output)
    {
        output.Clear();
        bool Routed(DeviceRole role) => profile.Route(role).Virtualized;
        var binding = profile.ModeBinding;
        modes.Update(binding is not null && State(binding.Role) is { Connected: true } bound && bound.Buttons[binding.Button - 1]);
        foreach (var map in profile.Axes)
        {
            var state = State(map.Role);
            if (state is not { Connected: true } || !Routed(map.Role)) continue;
            var value = transformer.Transform(state.Axes[(int)map.Source], modes.TransformFor(map.Target) ?? map.Transform);
            output.Axes[(int)map.Target] = modes.Inverts(map.Target) ? -value : value;
        }
        foreach (var map in modes.ButtonsForMode() ?? profile.Buttons)
        {
            if (binding is not null && binding.Role == map.Role && binding.Button == map.Source) continue;
            var state = State(map.Role);
            if (state is { Connected: true } && Routed(map.Role)) output.Buttons[map.Target - 1] |= state.Buttons[map.Source - 1];
        }
        foreach (var map in profile.Povs)
        {
            var state = State(map.Role);
            if (state is { Connected: true } && Routed(map.Role)) output.Povs[map.Target - 1] = state.Povs[map.Source - 1];
        }
    }
}
