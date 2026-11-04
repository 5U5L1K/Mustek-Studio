#define AppVersion "1.0.0"

[Setup]
AppId={{9DF0F16A-06C7-41A5-9F87-2FDF0241A74C}
AppName=Mustek Studio
AppVersion={#AppVersion}
AppVerName=Mustek Studio {#AppVersion}
AppPublisher=Mustek Studio
DefaultDirName={localappdata}\Programs\MustekStudio
DefaultGroupName=Mustek Studio
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupArchitecture=x64
MinVersion=10.0
WizardStyle=modern dynamic
OutputDir=..\release
OutputBaseFilename=MustekStudio-Setup
SetupIconFile=..\app\assets\mustek-scanner-app-icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
UninstallDisplayIcon={app}\MustekStudio.exe
CreateUninstallRegKey=not ValidationMode
UsePreviousAppDir=not ValidationMode
UsePreviousLanguage=no
DisableProgramGroupPage=yes
AllowNoIcons=yes
CloseApplications=yes
RestartApplications=no
InfoAfterFile=INSTALL-RU.txt
SetupLogging=yes

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
ru.DesktopIcon=Создать ярлык на рабочем столе
en.DesktopIcon=Create a desktop shortcut
ru.ScannerConnection=Подключение и настройка сканера
en.ScannerConnection=Scanner connection and setup
ru.FinishSetup=Завершить настройку сканера и открыть Mustek Studio
en.FinishSetup=Complete scanner setup and open Mustek Studio

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; Flags: unchecked

[Files]
Source: "..\release\MustekStudio-Portable.exe"; DestDir: "{app}"; DestName: "MustekStudio.exe"; Flags: ignoreversion
Source: "ScannerConnection.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\downloads\zadig-2.9.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "INSTALL-RU.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "INSTALL-EN.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "ZADIG-NOTICE.txt"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "..\runtime\ucrt64\share\licenses\libiconv\COPYING"; DestDir: "{app}\tools"; DestName: "COPYING-GPL3.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\Mustek Studio"; Filename: "{app}\MustekStudio.exe"; WorkingDir: "{app}"; Check: not ValidationMode
Name: "{group}\{cm:ScannerConnection}"; Filename: "{app}\ScannerConnection.exe"; Parameters: "--language {language}"; WorkingDir: "{app}"; Check: not ValidationMode
Name: "{group}\{cm:UninstallProgram,Mustek Studio}"; Filename: "{uninstallexe}"; Check: not ValidationMode
Name: "{autodesktop}\Mustek Studio"; Filename: "{app}\MustekStudio.exe"; WorkingDir: "{app}"; Tasks: desktopicon; Check: not ValidationMode

[Run]
Filename: "{app}\ScannerConnection.exe"; Parameters: "--finish --language {language}"; WorkingDir: "{app}"; Description: "{cm:FinishSetup}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallDelete]
Type: files; Name: "{app}\tools\zadig.ini"

[Code]
function ValidationMode: Boolean;
begin
  Result := ExpandConstant('{param:VALIDATE|0}') = '1';
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpInfoAfter) and (ActiveLanguage = 'en') then
    WizardForm.InfoAfterMemo.Lines.LoadFromFile(ExpandConstant('{app}\INSTALL-EN.txt'));
end;
