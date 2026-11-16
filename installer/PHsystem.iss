#ifndef AppVersion
#define AppVersion "0.3.0"
#endif
[Setup]
AppId={{362D5A81-41DF-4C20-B125-5D438A426730}
AppName=PHsystem Next
AppVersion={#AppVersion}
AppPublisher=PHsystem Next contributors
DefaultDirName={localappdata}\Programs\PHsystem Next
DefaultGroupName=PHsystem Next
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=PHsystem-Next-{#AppVersion}-win-x64-preview
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\PHsystem.Desktop.exe
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Files]
Source: "..\artifacts\desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; Flags: unchecked
[Icons]
Name: "{group}\PHsystem Next"; Filename: "{app}\PHsystem.Desktop.exe"
Name: "{autodesktop}\PHsystem Next"; Filename: "{app}\PHsystem.Desktop.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\PHsystem.Desktop.exe"; Description: "Abrir PHsystem Next"; Flags: nowait postinstall skipifsilent
; Local snapshots are intentionally retained on uninstall. Deletion is available inside the app.
