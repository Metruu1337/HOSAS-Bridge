using HOSASBridge.App.Services;
using HOSASBridge.Infrastructure;

namespace HOSASBridge.App.ViewModels;

public sealed class ModeRow
{
    public string Name { get; set; } = "MODE";
    public string Parent { get; set; } = "NORMAL";
    public string InvertedAxes { get; set; } = "";
}
public sealed class ModesViewModel : ObservableObject
{
    private readonly ProfileSession session;
    private readonly BridgeRuntime runtime;
    private bool binding;
    private bool[] previous = new bool[128];
    public ObservableCollection<ModeRow> Modes { get; } = [];
    public Array Roles { get; } = Enum.GetValues<DeviceRole>();
    public DeviceRole BindingRole { get; set; } = DeviceRole.Right;
    public BindingBehavior Behavior { get; set; }
    public Array Behaviors { get; } = Enum.GetValues<BindingBehavior>();
    public string BaseMode { get; set; } = "NORMAL";
    public string ActiveMode { get; set; } = "MINIGUN";
    private string bindingText = "No button bound";
    public string BindingText { get => L.T(bindingText); private set => Set(ref bindingText, value); }
    public Command Bind { get; }
    public AsyncCommand Clear { get; }
    public AsyncCommand Save { get; }
    public ModesViewModel(ProfileSession session, BridgeRuntime runtime, Action<Exception> error)
    {
        this.session = session; this.runtime = runtime;
        Bind = new(() => { binding = true; previous = runtime.Snapshot?.Roles.GetValueOrDefault(BindingRole)?.Buttons.ToArray() ?? new bool[128]; BindingText = "Release buttons, then press a button on the selected controller…"; });
        Clear = new(() => { binding = false; session.Save(session.Current with { ModeBinding = null }); return Task.CompletedTask; }, error);
        Save = new(() => { Apply(); return Task.CompletedTask; }, error);
        session.Changed += Reload; Reload();
    }
    private void Reload()
    {
        Modes.Clear(); foreach (var m in session.Current.Modes) Modes.Add(new() { Name = m.Id, Parent = m.Parent ?? "", InvertedAxes = string.Join(",", m.InvertAxes) });
        if (session.Current.ModeBinding is { } b) { BindingRole = b.Role; Behavior = b.Behavior; BaseMode = b.BaseMode; ActiveMode = b.ActiveMode; BindingText = L.F("RIGHT button {0} · {1}", b.Button, L.T(b.Behavior.ToString())); }
        else { BindingText = "No button bound"; BaseMode = session.Current.Modes[0].Id; ActiveMode = session.Current.Modes.Last().Id; }
        Changed(nameof(Behavior)); Changed(nameof(BaseMode)); Changed(nameof(ActiveMode));
    }
    private void Apply() => session.Save(session.Current with
    {
        Modes = Modes.Select(m => (session.Current.Modes.FirstOrDefault(old => old.Id == m.Name.Trim()) ?? new ModeDefinition(m.Name.Trim(), null, [])) with { Id = m.Name.Trim(), Parent = string.IsNullOrWhiteSpace(m.Parent) ? null : m.Parent.Trim(), InvertAxes =
            m.InvertedAxes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Enum.Parse<VirtualAxis>).Distinct().ToArray() }).ToArray(),
        ModeBinding = session.Current.ModeBinding is { } b ? b with { Behavior = Behavior, BaseMode = BaseMode, ActiveMode = ActiveMode } : null
    });
    public void Tick()
    {
        var right = runtime.Snapshot?.Roles.GetValueOrDefault(BindingRole);
        if (!binding || right is null || !right.Connected) return;
        if (BaseMode == ActiveMode) { binding = false; BindingText = "Create a second mode before binding."; return; }
        for (var i = 0; i < right.Buttons.Length; i++)
            if (right.Buttons[i] && !previous[i])
            {
                binding = false;
                session.Save(session.Current with { ModeBinding = new(BindingRole, i + 1, BaseMode, ActiveMode, Behavior) });
                return;
            }
        previous = right.Buttons.ToArray();
    }
}
