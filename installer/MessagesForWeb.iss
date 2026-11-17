; Installer for Messages for Web (Inno Setup 6)
; Build: run ISCC.exe on this file after building the Release configuration.

#define AppName "Messages for Web"
#define AppVersion "1.1.0"
#define AppExe "MessagesForWeb.exe"
; Must match ToastAppId in Main.vb
#define AppUserModelId "DvirGassner.MessagesForWeb"
; The notification library derives this ID from the app ID. It is stored at run time in
; HKCU\Software\Classes\AppUserModelId\<app ID>\CustomActivator. Update it here if the app ID changes.
#define ToastActivatorClsid "8eb36a62-c0ae-ed15-00c4-e53e15cda355"

[Setup]
AppId={{62554D92-8DE2-450D-AD1D-B9A372054633}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Dvir Gassner
AppPublisherURL=https://github.com/dvirgassner/messagesforweb
DefaultDirName={localappdata}\Programs\MessagesForWeb
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=output
OutputBaseFilename=MessagesForWeb-Setup-{#AppVersion}
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no

[Tasks]
Name: "startup"; Description: "Start {#AppName} when I sign in to Windows (hidden in the tray)"

[Files]
Source: "..\bin\Release\MessagesForWeb.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\MessagesForWeb.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Windows shows the app's notifications, and sends clicks on them to the app, only if the
; Start Menu shortcut carries the app ID and the toast activator ID.
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; AppUserModelID: "{#AppUserModelId}"; AppUserModelToastActivatorCLSID: "{#ToastActivatorClsid}"
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExe}"; Parameters: "-startup"; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im {#AppExe}"; Flags: runhidden; RunOnceId: "StopApp"

[UninstallDelete]
; Browser data of the app (includes the pairing with the phone)
Type: filesandordirs; Name: "{app}\{#AppExe}.WebView2"
Type: filesandordirs; Name: "{localappdata}\ToastNotificationManagerCompat\Apps\{#AppUserModelId}"

[Registry]
; Created by the app at run time; removed on uninstall
Root: HKCU; Subkey: "Software\MessagesForWeb"; Flags: dontcreatekey uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\{#AppUserModelId}"; Flags: dontcreatekey uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CLSID\{{{#ToastActivatorClsid}}"; Flags: dontcreatekey uninsdeletekey

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  // Stop a running copy, so its files can be replaced
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im {#AppExe}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;