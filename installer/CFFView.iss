#define AppName    "CFF View"
#define AppVersion "1.0.0"
#define AppExe     "CFFView.exe"
#define AppPublisher "OpenTransport"
#define AppURL     "https://github.com/JimMarley420/cffview"
#define BuildDir   "..\cffview\bin\Release\net10.0-windows\publish"

[Setup]
AppId={{A3F2B1C4-7E5D-4F89-9A2B-3C6D8E1F0A5B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=.\output
OutputBaseFilename=CFFView-Setup-{#AppVersion}
SetupIconFile=..\cffview\Assets\app.ico
WizardImageFile=..\cffview\Assets\wizard-banner.bmp
WizardSmallImageFile=..\cffview\Assets\wizard-small.bmp
WizardImageStretch=yes
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=Horaires Transports Publics Suisses

[Languages]
Name: "french";    MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon";  Description: "Créer une icône sur le Bureau";    GroupDescription: "Icônes supplémentaires:"; Flags: unchecked
Name: "startupicon";  Description: "Lancer au démarrage de Windows";   GroupDescription: "Options de démarrage:";   Flags: unchecked

[Files]
; All published files
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";             Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"
Name: "{group}\Désinstaller {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";       Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Lancer au démarrage (option)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExe}"""; \
  Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove user data only if the user chooses (leave favorites.json by default)
Type: dirifempty; Name: "{app}"

[Code]
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  // Require Windows 10 or later (needed for .NET 10)
  if (Version.Major < 10) then
  begin
    MsgBox('CFF View nécessite Windows 10 ou une version plus récente.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
