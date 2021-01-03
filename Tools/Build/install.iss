#define private VendorName "IS"
#define private ApplicationName "VolumeCalculator"
#define private ApplicationVersion "1.68"
#define private BinPath "..\..\!!bin\"
#define private SourcePath = BinPath + "x64\Release\"
#define private RootPath = "..\..\"
#define private ObfuscatedPath SourcePath + "Confused"
#define private OutputPath BinPath + "Installers"

[CustomMessages]
MsgInstallingRedist =Installing Visual C++ Redistributable 2015...
MsgUninstallPreviousVersion =VolumeCalculator is already installed, remove the old version?

[Setup]
AppId={#ApplicationName}
AppName={#ApplicationName}
AppPublisher={#VendorName}
AppVersion={#ApplicationVersion}
DefaultDirName={pf}\{#ApplicationName}
DefaultGroupName={#VendorName} {#ApplicationName}
UninstallDisplayIcon={app}\{#ApplicationName}.exe
OutputBaseFilename={#ApplicationName}_{#ApplicationVersion}_Setup
Compression=lzma2
SolidCompression=yes
OutputDir={#OutputPath}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: {#ObfuscatedPath}\*; DestDir: "{app}";  Flags: ignoreversion; BeforeInstall: KillExecutables()
Source: {#SourcePath}Microsoft.Kinect.dll; DestDir: "{app}"
Source: {#SourcePath}Fleck.dll; DestDir: "{app}"
Source: {#SourcePath}IPCameraTest.exe; DestDir: "{app}"
Source: {#SourcePath}libD435FrameProvider.dll; DestDir: "{app}"
Source: {#SourcePath}libDepthMapProcessor.dll; DestDir: "{app}"
Source: {#SourcePath}opencv_world310.dll; DestDir: "{app}"
Source: {#SourcePath}realsense2.dll; DestDir: "{app}"
Source: {#RootPath}packages\NOTICE.txt; DestDir: "{app}"
Source: {#RootPath}web\*; DestDir: "C:\web\"; Flags: recursesubdirs ignoreversion; BeforeInstall: TaskKill('nginx.exe')

; VC++ redistributable runtime. Extracted by VC2017RedistNeedsInstall(), if needed.
Source: {#RootPath}Externals\vc_redist.x64.exe; DestDir: {tmp}; Flags: dontcopy

[Icons]
Name: {group}\{#ApplicationName}; Filename: {app}\{#ApplicationName}.exe
Name: {group}\VCConfigurator; Filename: {app}\VCConfigurator.exe
Name: {userdesktop}\{#ApplicationName}; Filename: {app}\{#ApplicationName}.exe
Name: {userdesktop}\VCConfigurator; Filename: {app}\VCConfigurator.exe

[Run]
Filename: "schtasks"; Parameters: "/Create /f /rl highest /sc onlogon /tr ""'{app}\{#ApplicationName}.exe'"" /tn ""RunVCalc""";
Filename: "schtasks"; Parameters: "/Create /f /rl highest /sc onlogon /tr ""'C:\web\nginx-1.15.8\nginx.exe'"" /tn ""RunVCalcWeb""";
Filename: "{tmp}\vc_redist.x64.exe"; StatusMsg: "{cm:MsgInstallingRedist}"; Parameters: "/quiet"; Check: VC2017RedistNeedsInstall ; Flags: waituntilterminated
Filename: "C:\web\nginx-1.15.8\nginx.exe"; Flags: nowait

[UninstallRun]
Filename: "schtasks"; Parameters: "/Delete /f /tn ""RunVCalc""";
Filename: "schtasks"; Parameters: "/Delete /f /tn ""RunVCalcWeb""";
Filename: "taskkill"; Parameters: "/im ""VolumeCalculator.exe"" /f"; Flags: runhidden
Filename: "taskkill"; Parameters: "/im ""VCConfigurator.exe"" /f"; Flags: runhidden
Filename: "taskkill"; Parameters: "/im ""nginx.exe"" /f"; Flags: runhidden

[Code]
function VC2017RedistNeedsInstall: Boolean;
var 
	Version: String;
begin
	if (RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Version', Version)) then
	begin
		// Is the installed version at least 14.14 ? 
		Log('VC Redist Version check : found ' + Version);
		Result := (CompareStr(Version, 'v14.14.26429.03')<0);
	end
	else 
	begin
		// Not even an old version installed
		Result := True;
	end;
	if (Result) then
	begin
		ExtractTemporaryFile('vc_redist.x64.exe');
	end;
end;

procedure TaskKill(FileName: String);
var
	ResultCode: Integer;
begin
	Exec(ExpandConstant('taskkill.exe'), '/f /im ' + '"' + FileName + '"', '', SW_HIDE,
	ewWaitUntilTerminated, ResultCode);
end;

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

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UninstallPreviousVersion(): Integer;
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

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UninstallPreviousVersion();
    end;
  end;
end;

procedure KillExecutables();
begin
    TaskKill('VolumeCalculator.exe');
	TaskKill('VCConfigurator.exe');
end;