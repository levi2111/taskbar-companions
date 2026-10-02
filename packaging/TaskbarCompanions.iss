; Installer for Taskbar Companions. Installs for the current Windows account only and never asks for
; administrator rights. packaging\build.ps1 compiles it and passes the version and the staged files.

#ifndef AppVersion
  #error Pass /DAppVersion=x.y.z; packaging\build.ps1 does this.
#endif
#ifndef StageDir
  #error Pass /DStageDir=<staged files>; packaging\build.ps1 does this.
#endif

#define AppName "Taskbar Companions"
#define AppExe "TaskbarCompanions.exe"
#define AppUrl "https://github.com/levi2111/taskbar-companions"

[Setup]
; Never change AppId: Windows uses it to recognize this app for updates and uninstalling.
AppId={{A7E3C1D9-2F4B-4E8A-9B6D-5C3E1F7A2B94}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=levi2111
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
AppCopyright=Copyright (c) 2026 levi2111
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} Setup
; Per user, under %LOCALAPPDATA%\Programs.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
LicenseFile={#StageDir}\LICENSE.txt
InfoBeforeFile=before-install.txt
SetupIconFile=..\TaskbarCompanions\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
; The app holds this mutex while it runs, so Setup and the uninstaller ask you to quit it first.
AppMutex=TaskbarCompanions.Default
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
OutputBaseFilename=TaskbarCompanions-Setup-{#AppVersion}-x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start {#AppName} when I sign in to Windows"; GroupDescription: "When Windows starts:"; Flags: unchecked

[InstallDelete]
; Unticking an option when reinstalling removes the shortcut an earlier install added.
Type: files; Name: "{userstartup}\{#AppName}.lnk"; Tasks: not startup
Type: files; Name: "{autodesktop}\{#AppName}.lnk"; Tasks: not desktopicon

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; The app's settings, saved positions and bridge files.
Type: filesandordirs; Name: "{localappdata}\TaskbarCompanions"
