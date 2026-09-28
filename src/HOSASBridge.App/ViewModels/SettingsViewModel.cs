using HOSASBridge.Infrastructure;

namespace HOSASBridge.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore store;
    private readonly IApplicationStartupService startup;
    public UserSettings Current { get; private set; }
    public string Language { get; set; }
    public string[] Languages { get; } = ["Polski", "English"];
    public bool StartWithWindows { get; set; }
    public bool AutoStart { get; set; }
    public bool StartMinimized { get; set; }
    public bool CloseToTray { get; set; }
    public bool AudioCues { get; set; }
    public bool AutoActivateProcess { get; set; }
    public string LogLevel { get; set; }
    public string[] LogLevels { get; } = ["Debug", "Information", "Warning", "Error"];
    public AsyncCommand Save { get; }
    public SettingsViewModel(SettingsStore store, IApplicationStartupService startup, Action<Exception> error, Action<string> notify)
    {
        this.store = store; this.startup = startup; Current = store.Load();
        Language = Current.Language == "en" ? "English" : "Polski";
        StartWithWindows = startup.Enabled; AutoStart = Current.StartBridgeAutomatically; StartMinimized = Current.StartMinimized;
        CloseToTray = Current.CloseToTray; AudioCues = Current.AudioCues; AutoActivateProcess = Current.AutoActivateOnProcess; LogLevel = Current.LogLevel;
        Save = new(() =>
        {
            var languageChanged = Current.Language != (Language == "English" ? "en" : "pl");
            startup.SetEnabled(StartWithWindows, Environment.ProcessPath!);
            Current = Current with { Language = Language == "English" ? "en" : "pl", StartBridgeAutomatically = AutoStart, StartMinimized = StartMinimized, CloseToTray = CloseToTray, AudioCues = AudioCues, LogLevel = LogLevel, AutoActivateOnProcess = AutoActivateProcess };
            store.Save(Current); notify(languageChanged ? "Settings saved. Restart the application to change language." : "Settings saved."); return Task.CompletedTask;
        }, error);
    }
    public void CompleteSetup() { Current = Current with { SetupCompleted = true }; store.Save(Current); }
    public void ResetSetup() { Current = Current with { SetupCompleted = false }; store.Save(Current); }
}
