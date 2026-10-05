#define MyAppName "Book Shelf"
#define MyAppVersion "1.0"
#define MyAppPublisher "Martin De Leon"
#define MyAppURL "https://github.com/Parkaboy/Book_Shelf"
#define MyAppExeName "Book_Shelf.exe"
#if FileExists("artifacts\publish\win-x64\Book_Shelf.exe")
  #define PublishDir "artifacts\publish\win-x64"
#elif FileExists("..\..\..\artifacts\publish\win-x64\Book_Shelf.exe")
  #define PublishDir "..\..\..\artifacts\publish\win-x64"
#else
  #error "Published app files not found. Run dotnet publish from the repository root before compiling this installer."
#endif

[Setup]
AppId={{E3D0A376-CA3C-4A6D-A107-913FF53F8E36}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
OutputBaseFilename=BookShelfSetup
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
