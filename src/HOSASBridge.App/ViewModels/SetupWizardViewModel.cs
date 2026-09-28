using HOSASBridge.App.Services;
using HOSASBridge.Profiles;
namespace HOSASBridge.App.ViewModels;

public sealed class SetupWizardViewModel : ObservableObject
{
    private int step;
    private static readonly string[] Titles = ["Welcome", "Dependency health", "Detect controllers", "Assign roles", "Choose preset", "Live input test", "Optional mode binding", "Health verification", "Ready"];
    public int Step { get => step; private set { if (Set(ref step, value)) { Changed(nameof(Title)); Changed(nameof(Progress)); for (var i = 0; i < 9; i++) Changed("Step" + i); } } }
    public string Title => L.T(Titles[Step]);
    public string Progress => $"{Step + 1} / 9";
    public bool Step0 => Step == 0; public bool Step1 => Step == 1; public bool Step2 => Step == 2;
    public bool Step3 => Step == 3; public bool Step4 => Step == 4; public bool Step5 => Step == 5;
    public bool Step6 => Step == 6; public bool Step7 => Step == 7; public bool Step8 => Step == 8;
    public string[] Presets => PresetCatalog.Names;
    public string SelectedPreset { get; set; } = PresetCatalog.Names[0];
    public Command Next { get; }
    public Command Back { get; }
    public AsyncCommand ApplyPreset { get; }
    public SetupWizardViewModel(ProfileSession session, Action<Exception> error, Action<string> notify)
    {
        Next = new(() => Step = Math.Min(8, Step + 1)); Back = new(() => Step = Math.Max(0, Step - 1));
        ApplyPreset = new(() => { session.Save(PresetCatalog.Create(SelectedPreset) with { Devices = session.Current.Devices, VirtualDevice = session.Current.VirtualDevice }); notify("Preset applied. Review mappings before starting."); return Task.CompletedTask; }, error);
    }
    public void Reset() => Step = 0;
}
