#ifndef AppVersion
  #define AppVersion "0.8.1"
#endif
#define Payload "..\artifacts\installer-payload"
#ifdef InstallerSmokeTest
  #define AppIdentity "{{E411BAEC-ABF3-445F-9B48-2FC2D5407B88}"
  #define StateDir "{app}\SandboxProfile\WinCalendar"
  #define MenuDir "{app}\SandboxProfile\Programs\WinCalendar"
  #define DesktopDir "{app}\SandboxProfile\Desktop"
  #define RunSubkey "Software\WinCalendar\InstallerSmoke\Run"
#else
  #define AppIdentity "{{66F7A8B9-8407-45BD-81ED-A04C577AE159}"
  #define StateDir "{localappdata}\WinCalendar"
  #define MenuDir "{userprograms}\WinCalendar"
  #define DesktopDir "{userdesktop}"
  #define RunSubkey "Software\Microsoft\Windows\CurrentVersion\Run"
#endif

[Setup]
AppId={#AppIdentity}
AppName=WinCalendar
AppVersion={#AppVersion}
AppPublisher=WinCalendar
AppPublisherURL=https://github.com/sergik15828/WinCalendar
AppSupportURL=https://github.com/sergik15828/WinCalendar/issues
AppUpdatesURL=https://github.com/sergik15828/WinCalendar/releases
DefaultDirName={localappdata}\Programs\WinDate
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible and not arm64
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\dist\releases
OutputBaseFilename=WinCalendar-{#AppVersion}-Setup-x64
SetupIconFile=..\dist\WinCalendar.ico
UninstallDisplayIcon={app}\WinDate.exe
VersionInfoVersion={#AppVersion}.0
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
UsePreviousTasks=yes
#ifdef InstallerSmokeTest
UsePreviousAppDir=no
#endif

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.Options=Дополнительно:
english.Options=Options:
russian.Startup=Запускать вместе с Windows
english.Startup=Start with Windows
russian.Desktop=Создать ярлык на рабочем столе
english.Desktop=Create a desktop shortcut
russian.Launch=Запустить WinCalendar
english.Launch=Launch WinCalendar
russian.Guide=Инструкция по настройке
english.Guide=Getting started
russian.NetRequired=Для WinCalendar требуется .NET Framework 4.8 или новее.
english.NetRequired=WinCalendar requires .NET Framework 4.8 or newer.
russian.CloseApp=Закройте WinCalendar и повторите установку.
english.CloseApp=Close WinCalendar and try the installation again.

[Tasks]
Name: "startup"; Description: "{cm:Startup}"; GroupDescription: "{cm:Options}"
Name: "desktopicon"; Description: "{cm:Desktop}"; GroupDescription: "{cm:Options}"; Flags: unchecked

[Files]
Source: "{#Payload}\WinDate.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\WinDate.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\Settings.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\EnableWeatherPrototype.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\DisableWeatherPrototype.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\GETTING_STARTED.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\GOOGLE_SETUP.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{#MenuDir}\WinCalendar"; Filename: "{app}\WinDate.exe"; WorkingDir: "{app}"
Name: "{#MenuDir}\WinCalendar Settings"; Filename: "{app}\WinDate.exe"; Parameters: "--menu"; WorkingDir: "{app}"
Name: "{#MenuDir}\{cm:Guide}"; Filename: "{sys}\notepad.exe"; Parameters: """{app}\GETTING_STARTED.txt"""
Name: "{#StateDir}\Startup"; Filename: "{app}\WinDate.exe"; Parameters: "--background"; WorkingDir: "{app}"
Name: "{#DesktopDir}\WinCalendar"; Filename: "{app}\WinDate.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "{#RunSubkey}"; ValueType: string; ValueName: "WinCalendar"; ValueData: """{win}\explorer.exe"" ""{#StateDir}\Startup.lnk"""; Tasks: startup

[Run]
Filename: "{win}\explorer.exe"; Parameters: """{app}\WinDate.exe"""; Description: "{cm:Launch}"; Flags: nowait postinstall skipifsilent

[Code]
function OwnStartupCommand(Value: String): Boolean;
begin
  Result := (Pos(Lowercase(ExpandConstant('{#StateDir}\Startup.lnk')), Lowercase(Value)) > 0) or
    (Pos(Lowercase(ExpandConstant('{app}\WinDate.exe')), Lowercase(Value)) > 0);
end;

procedure RemoveOwnStartup;
var Value: String;
begin
  if RegQueryStringValue(HKCU, '{#RunSubkey}', 'WinCalendar', Value) and OwnStartupCommand(Value) then
    RegDeleteValue(HKCU, '{#RunSubkey}', 'WinCalendar');
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNetInstalled(net48, 0);
  if not Result then
    SuppressibleMsgBox(CustomMessage('NetRequired'), mbCriticalError, MB_OK, IDOK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
#ifndef InstallerSmokeTest
var ExitCode, Attempt: Integer;
#endif
begin
  Result := '';
#ifndef InstallerSmokeTest
  if CheckForMutexes('Local\WinCalendar.Desktop.Prototype') then begin
    ExtractTemporaryFile('WinDate.exe');
    if not Exec(ExpandConstant('{tmp}\WinDate.exe'), '--exit', ExpandConstant('{tmp}'), SW_HIDE, ewWaitUntilTerminated, ExitCode) then begin
      Result := CustomMessage('CloseApp');
      Exit;
    end;
    Attempt := 0;
    while CheckForMutexes('Local\WinCalendar.Desktop.Prototype') and (Attempt < 50) do begin
      Sleep(100);
      Attempt := Attempt + 1;
    end;
    if CheckForMutexes('Local\WinCalendar.Desktop.Prototype') then Result := CustomMessage('CloseApp');
  end;
#endif
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not WizardIsTaskSelected('startup') then RemoveOwnStartup;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
#ifndef InstallerSmokeTest
var ExitCode: Integer;
#endif
begin
  if CurUninstallStep = usUninstall then begin
#ifndef InstallerSmokeTest
    if FileExists(ExpandConstant('{app}\WinDate.exe')) then begin
      Exec(ExpandConstant('{app}\WinDate.exe'), '--exit', ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ExitCode);
      Sleep(500);
    end;
#endif
    RemoveOwnStartup;
  end;
#ifdef InstallerSmokeTest
  if CurUninstallStep = usPostUninstall then RegDeleteKeyIfEmpty(HKCU, '{#RunSubkey}');
#endif
end;
