; ZoneShift installer - compiled by Inno Setup 6
; Pass architecture defines from pack-installer.ps1:
;   ISCC /DAppArch=x64 /DPublishDir=..\publish\win-x64 /DMyAppVersion=1.6.2 ZoneShift.iss
;   ISCC /DAppArch=arm64 /DPublishDir=..\publish\win-arm64 /DMyAppVersion=1.6.2 ZoneShift.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.6.2"
#endif
#ifndef AppArch
  #define AppArch "x64"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish\win-x64"
#endif

#define MyAppName "ZoneShift"
#define MyAppPublisher "ZoneShift"
#define MyAppExeName "ZoneShift.exe"

[Setup]
AppId={{A7C3E9F1-4B2D-4E8A-9C1F-6D5E8A2B3C40}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} ({#AppArch})
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=ZoneShift-Setup-{#MyAppVersion}-{#AppArch}
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
#if AppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
MinVersion=10.0
CloseApplications=yes
; Restart apps closed by CloseApplications (helps when the running ZoneShift is force-closed)
RestartApplications=yes
ChangesAssociations=no
AllowNoIcons=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start ZoneShift when I sign in to Windows"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: startupicon

[Run]
; Interactive wizard: optional "Launch ZoneShift" checkbox
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; Silent auto-update (/VERYSILENT): always relaunch after a short delay so the old
; process can release its single-instance mutex before the new exe starts.
Filename: "{sys}\cmd.exe"; Parameters: "/C ping 127.0.0.1 -n 3 >nul & start """" ""{app}\{#MyAppExeName}"""; WorkingDir: "{app}"; Flags: nowait postinstall skipifnotsilent runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ZoneShift"
