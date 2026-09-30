#define AppVersion "0.9.0-beta.4"
[Setup]
SetupIconFile=..\src\HOSASBridge.App\Assets\bridge.ico
AppId={{E524A77C-B48A-4E13-8E38-FA9E3A97668D}
AppName=HOSAS Bridge
AppVersion={#AppVersion}
AppPublisher=HOSAS Bridge
DefaultDirName={autopf}\HOSAS Bridge
DefaultGroupName=HOSAS Bridge
OutputDir=..\artifacts\installer
OutputBaseFilename=HOSASBridge-{#AppVersion}-win-x64-Setup
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\HOSASBridge.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
LicenseFile=..\LICENSE

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
polish.InstallVjoy=Zainstaluj / napraw podpisany vJoy 2.2.2 (współdzielony sterownik kontrolera)
english.InstallVjoy=Install / repair signed vJoy 2.2.2 (shared controller driver)
polish.InstallHidhide=Zainstaluj / napraw podpisany HidHide 1.5.230 (współdzielony sterownik widoczności)
english.InstallHidhide=Install / repair signed HidHide 1.5.230 (shared visibility driver)
polish.Desktop=Skrót na pulpicie
english.Desktop=Create a desktop shortcut
polish.Launch=Uruchom konfigurację HOSAS Bridge
english.Launch=Launch HOSAS Bridge setup

polish.RestoreHiding=Przywrócić reguły ukrywania zarządzane przez HOSAS Bridge? Współdzielone sterowniki pozostaną zainstalowane.
english.RestoreHiding=Restore device-hiding rules managed by HOSAS Bridge? Shared drivers will remain installed.
polish.DeleteSettings=Usunąć ustawienia, profile i logi HOSAS Bridge bieżącego użytkownika?
english.DeleteSettings=Delete HOSAS Bridge settings, profiles and logs for the current user?

[Tasks]
Name: desktopicon; Description: "{cm:Desktop}"; Flags: unchecked

[Files]
Source: "..\artifacts\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\ARCHITECTURE.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\DEPENDENCIES.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\BUILDING.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\TROUBLESHOOTING.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\TESTING.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\HOSAS Bridge"; Filename: "{app}\HOSASBridge.exe"
Name: "{autodesktop}\HOSAS Bridge"; Filename: "{app}\HOSASBridge.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\HOSASBridge.exe"; Description: "{cm:Launch}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
// Driver setup is explicit in the application Devices page; never nest it in app installation.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then begin
    if FileExists(ExpandConstant('{localappdata}\HOSASBridge\hiding-ownership.json')) then
      if MsgBox(CustomMessage('RestoreHiding'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        if not Exec(ExpandConstant('{app}\HOSASBridge.exe'), '--setup-operation restore "' + ExpandConstant('{localappdata}\HOSASBridge\profile.json') + '"', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then RaiseException('Could not restore hiding rules.');
        if ResultCode <> 0 then RaiseException('Hiding restoration failed. Your settings have been retained.');
      end;
    if MsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      if not FileExists(ExpandConstant('{localappdata}\HOSASBridge\hiding-ownership.json')) then
        DelTree(ExpandConstant('{localappdata}\HOSASBridge'), True, True, True);
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'HOSASBridge');
  end;
end;
