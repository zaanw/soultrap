#define AppVersion "0.1.0"
#ifndef PublishDir
#define PublishDir "..\artifacts\publish"
#endif
[Setup]
AppId={{6D50B263-DF52-4623-8142-82B0399FA01D}
AppName=Soultrap Ultimate Aero
AppVersion={#AppVersion}
AppPublisher=Soultrap contributors
DefaultDirName={localappdata}\Programs\Soultrap
DefaultGroupName=Soultrap
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\artifacts\installer
OutputBaseFilename=Soultrap-0.1.0-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=classic
UninstallDisplayIcon={app}\Soultrap.exe
#ifdef SignedBuild
SignTool=soultrap
SignedUninstaller=yes
#endif
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Soultrap"; Filename: "{app}\Soultrap.exe"
Name: "{group}\Uninstall Soultrap"; Filename: "{uninstallexe}"
[UninstallDelete]
; User settings and rollback backups are intentionally retained.
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "Soultrap"; Flags: uninsdeletevalue
