#define private VendorName "IS"
#define private ApplicationName "VolumeCalculator"
#define private ApplicationVersion "1.44"
#define private BinPath "..\..\!!bin\"
#define private SourcePath = BinPath + "x64\Release\"
#define private RootPath = "..\..\"
#define private ObfuscatedPath SourcePath + "Confused"
#define private OutputPath BinPath + "Installers"

[CustomMessages]
InstallingRedist =Installing Visual C++ Redistributable 2015...

[Setup]
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
Source: {#ObfuscatedPath}\*; DestDir: "{app}";  Flags: ignoreversion; BeforeInstall: TaskKill('VolumeCalculator.exe')
Source: {#SourcePath}Microsoft.Kinect.dll; DestDir: "{app}"
Source: {#SourcePath}WinSCP.exe; DestDir: "{app}"
Source: {#SourcePath}WinSCPnet.dll; DestDir: "{app}"
Source: {#SourcePath}LibUsbDotNet.LibUsbDotNet.dll; DestDir: "{app}"
Source: {#SourcePath}Fleck.dll; DestDir: "{app}"
Source: {#SourcePath}VCConfigurator.exe; DestDir: "{app}"
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
Name: {commondesktop}\{#ApplicationName}; Filename: {app}\{#ApplicationName}.exe

[Run]
Filename: "schtasks"; Parameters: "/Create /f /rl highest /sc onlogon /tr ""'{app}\{#ApplicationName}.exe'"" /tn ""RunVCalc""";
Filename: "schtasks"; Parameters: "/Create /f /rl highest /sc onlogon /tr ""'C:\web\nginx-1.15.8\nginx.exe'"" /tn ""RunVCalcWeb""";
Filename: "{tmp}\vc_redist.x64.exe"; StatusMsg: "{cm:InstallingRedist}"; Parameters: "/quiet"; Check: VC2017RedistNeedsInstall ; Flags: waituntilterminated
Filename: "C:\web\nginx-1.15.8\nginx.exe"; Flags: nowait

[UninstallRun]
Filename: "schtasks"; Parameters: "/Delete /f /tn ""RunVCalc""";
Filename: "schtasks"; Parameters: "/Delete /f /tn ""RunVCalcWeb""";
Filename: "taskkill"; Parameters: "/im ""VolumeCalculator.exe"" /f"; Flags: runhidden
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


