; ZoneShift installer - compiled by Inno Setup 6
; Pass architecture defines from pack-installer.ps1:
;   ISCC /DAppArch=x64 /DPublishDir=..\publish\win-x64 /DMyAppVersion=1.4.0 ZoneShift.iss
;   ISCC /DAppArch=arm64 /DPublishDir=..\publish\win-arm64 /DMyAppVersion=1.4.0 ZoneShift.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.4.1"
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
RestartApplications=no
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
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ZoneShift"
