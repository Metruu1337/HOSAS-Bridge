using HOSASBridge.Core;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace HOSASBridge.Infrastructure;

[SupportedOSPlatform("windows")]
public sealed class WindowsStartupService : IApplicationStartupService
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public bool Enabled { get { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue("HOSASBridge") is string; } }
    public void SetEnabled(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) key.SetValue("HOSASBridge", $"\"{executable}\" --startup"); else key.DeleteValue("HOSASBridge", false);
    }
}
