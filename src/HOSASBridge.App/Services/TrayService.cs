using HOSASBridge.App.ViewModels;
using Forms = System.Windows.Forms;
using System.Drawing;
using System.Windows;

namespace HOSASBridge.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon icon;
    public TrayService(Window window, MainViewModel model, Action exit)
    {
        void Open() { window.Show(); window.WindowState = WindowState.Normal; window.Activate(); }
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(L.T("Open HOSAS Bridge"), null, (_, _) => Open());
        var profileItem = menu.Items.Add(model.ProfileName); profileItem.Enabled = false;
        menu.Items.Add(L.T("Diagnostics"), null, (_, _) => { model.SelectedTab = 5; Open(); });
        menu.Items.Add(L.T("Start / Stop Bridge"), null, (_, _) => { if (model.IsRunning) model.Stop.Execute(null); else model.Start.Execute(null); });
        var normalItem = menu.Items.Add(model.BaseModeLabel, null, (_, _) => model.Normal.Execute(null));
        var activeItem = menu.Items.Add(model.ActiveModeLabel, null, (_, _) => model.Minigun.Execute(null));
        menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add(L.T("Exit"), null, (_, _) => exit());
        icon = new Forms.NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application, Text = "HOSAS Bridge · " + L.T("NORMAL"), ContextMenuStrip = menu, Visible = true };
        icon.DoubleClick += (_, _) => Open();
        model.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(model.Mode) or nameof(model.Bridge) or nameof(model.ProfileName)) { normalItem.Text = model.BaseModeLabel; activeItem.Text = model.ActiveModeLabel; profileItem.Text = model.ProfileName; var text = $"HOSAS Bridge · {model.ProfileName} · {L.T(model.Mode)} · {model.Bridge}"; icon.Text = text[..Math.Min(63, text.Length)]; } };
        model.ModeChanged += mode =>
        {
            if (model.Settings.Current.AudioCues) _ = Task.Run(() => Console.Beep(mode == "NORMAL" ? 440 : 880, 75));
        };
    }
    public void Dispose() { icon.Visible = false; icon.ContextMenuStrip?.Dispose(); icon.Dispose(); }
}
