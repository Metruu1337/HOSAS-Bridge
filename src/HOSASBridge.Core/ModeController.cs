namespace HOSASBridge.Core;

public sealed class ModeController : IModeController
{
    private readonly Dictionary<string, bool[]> inversions;
    private readonly ModeBinding? binding;
    private readonly Dictionary<string, ModeDefinition> definitions;
    private bool wasPressed;
    public string Current { get; private set; }
    public ModeController(Profile profile)
    {
        binding = profile.ModeBinding;
        definitions = profile.Modes.ToDictionary(m => m.Id);
        inversions = new(StringComparer.Ordinal);
        bool[] Resolve(string id, HashSet<string> visiting)
        {
            if (inversions.TryGetValue(id, out var cached)) return cached;
            if (!visiting.Add(id)) throw new ArgumentException("Mode inheritance cycle.");
            var mode = definitions[id];
            var result = mode.Parent is null ? new bool[8] : (bool[])Resolve(mode.Parent, visiting).Clone();
            foreach (var axis in mode.InvertAxes) result[(int)axis] ^= true;
            visiting.Remove(id); inversions[id] = result; return result;
        }
        foreach (var mode in profile.Modes) Resolve(mode.Id, []);
        Current = profile.Modes[0].Id;
    }
    public void Select(string id)
    {
        if (!inversions.ContainsKey(id)) throw new ArgumentException("Unknown mode.", nameof(id));
        Current = id;
    }
    public void Update(bool pressed)
    {
        if (binding is not null)
        {
            if (binding.Behavior == BindingBehavior.Hold) Current = pressed ? binding.ActiveMode : binding.BaseMode;
            else if (pressed && !wasPressed) Current = Current == binding.ActiveMode ? binding.BaseMode : binding.ActiveMode;
        }
        wasPressed = pressed;
    }
    public bool Inverts(VirtualAxis axis) => inversions[Current][(int)axis];
    public AxisTransform? TransformFor(VirtualAxis axis)
    {
        for (string? id = Current; id is not null; id = definitions[id].Parent)
            if (definitions[id].AxisOverrides.TryGetValue(axis, out var transform)) return transform;
        return null;
    }
    public ButtonMapping[]? ButtonsForMode()
    {
        for (string? id = Current; id is not null; id = definitions[id].Parent)
            if (definitions[id].ButtonOverrides is { } buttons) return buttons;
        return null;
    }
}
