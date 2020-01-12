#define ApplicationName 'Levrum DataBridge'
#define ApplicationVersion GetFileVersion('..\DataBridge\bin\Release\DataBridge.exe')

[Setup]
AppId=C5DCBCF2-D071-466E-B3AD-B0771A01C270
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher=Levrum
DefaultDirName={localappdata}\Levrum\{#ApplicationName}
DefaultGroupName={#ApplicationName}
UninstallDisplayIcon={app}\DataBridge.exe
Compression=lzma2
SolidCompression=yes
AllowNoIcons=yes
WizardSmallImageFile="Small Install Image.bmp"
WizardImageFile="Big Install Image.bmp"
ChangesAssociations=yes
;InfoBeforeFile="..\Dependencies\Changelog.txt"

[Dirs]
Name:"{app}\plugins"; Permissions:everyone-modify; Flags: uninsneveruninstall

[Files]
Source: "..\DataBridge\bin\Release\*"; DestDir: "{app}"; Flags: recursesubdirs overwritereadonly ignoreversion

;Redistributables
Source: "..\Dependencies\windowsdesktop-runtime-3.1.0-win-x64.exe"; DestDir: {tmp}; Flags: ignoreversion deleteafterinstall
Source: "..\Dependencies\vc_redist.x64.exe"; DestDir: {tmp}; Flags: ignoreversion deleteafterinstall

[Tasks]
Name: desktopicon; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"

[Icons]
Name: "{group}\{#ApplicationName}"; Filename: "{app}\netcoreapp3.1\DataBridge.exe"; IconFilename: "{app}\netcoreapp3.1\databridge.ico"
Name: "{commondesktop}\{#ApplicationName}"; Filename: "{app}\netcoreapp3.1\DataBridge.exe"; IconFilename: "{app}\netcoreapp3.1\databridge.ico"; Tasks: desktopicon

[Run]
Filename: {tmp}\windowsdesktop-runtime-3.1.0-win-x64.exe; StatusMsg: "Installing Microsoft .NET Core 3.1"; Description: Install Microsoft .NET Core 3.1; Parameters: /passive /noreboot; Flags: skipifdoesntexist; Check: ShouldInstalldotNETCore31
Filename: {tmp}\vc_redist.x64.exe; StatusMsg: "Installing Microsoft Visual C++ 2019 Redistributable"; Description: Install Visual C++ 2019 Redistributable; Parameters: /passive /noreboot; Flags: skipifdoesntexist; Check: ShouldInstallVCRedist
Filename: {app}\netcoreapp3.1\DataBridge.exe; Description: {cm:LaunchProgram,{cm:AppName}}; Flags: nowait postinstall skipifsilent

[CustomMessages]
AppName=Levrum DataBridge
LaunchProgram=Start Levrum DataBridge after finishing installation


[Registry]
Root: HKCR; Subkey: ".dmap"; ValueData: "{#ApplicationName}"; Flags: uninsdeletevalue; ValueType: string; ValueName: "";
Root: HKCR; Subkey: "{#ApplicationName}"; ValueData: "Levrum DataMap";  Flags: uninsdeletekey; ValueType: string; ValueName: "";
Root: HKCR; Subkey: "{#ApplicationName}\DefaultIcon"; ValueData: "{app}\netcoreapp3.1\datamap.ico"; ValueType: string; ValueName: "";
Root: HKCR; Subkey: "{#ApplicationName}\shell\open\command"; ValueData: """{app}\netcoreapp3.1\DataBridge.exe"" ""%1"""; ValueType: string; ValueName: "";

[Code]
/////////////////////////////////////////////////////////////////////
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

/////////////////////////////////////////////////////////////////////
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;

/////////////////////////////////////////////////////////////////////
var dotNETCore31Missing: Boolean; // Is the .NET Core 3.1 missing entirely?
var vc2019Missing: Boolean; // Is the VC++ 2019 Redistributable missing?
var dotNETVersion: Cardinal;
var vcVersion: String;

function InitializeSetup(): Boolean;
begin
    // Test the presence of .NET Core 3.1
    if (RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App') and RegQueryDWordValue(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App', '3.1.0', dotNETVersion)) then
    begin
        if (dotNETVersion >= 1) then
          dotNETCore31Missing := False
        else
          dotNETCore31Missing := True;
    end else
      dotNETCore31Missing := True;

    if (not(RegValueExists(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Version'))) then
        vc2019Missing := True
    else
    begin
      if (not(RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Version', vcVersion))) then
        vc2019Missing := True;

      if (vcVersion <> 'v14.23.27280.00') then
        vc2019Missing := True
      else
        vc2019Missing := False;
    end;
    Result := True;
end;

function ShouldInstalldotNETCore31(): Boolean;
begin
    Result := dotNETCore31Missing;
end;

function ShouldInstallVCRedist(): Boolean;
begin
    Result := vc2019Missing;
end;