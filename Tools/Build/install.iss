#define private VendorName "IS"
#define private ApplicationName "VolumeCalculator"
#define private ApplicationVersion "1.29"
#define private ApplicationEdition={param:edition|standard}
#define private BinPath "..\..\!!bin\"
#define private SourcePath = BinPath + "x64\Release\"
#define private ObfuscatedPath SourcePath + "Confused"
#define private OutputPath BinPath + "Installers"

[Setup]
AppName={#ApplicationName} {#ApplicationEdition} {#ApplicationVersion}
AppPublisher={#VendorName}
AppVersion={#ApplicationVersion}
DefaultDirName={pf}\{#ApplicationName}
DefaultGroupName={#VendorName} {#ApplicationName} {#ApplicationEdition}
UninstallDisplayIcon={app}\{#ApplicationName}.exe
OutputBaseFilename={#ApplicationName}_{#ApplicationVersion}_Setup
Compression=lzma2
SolidCompression=yes
OutputDir={#OutputPath}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: {#ObfuscatedPath}\*; DestDir: "{app}";
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

[Icons]
Name: {group}\{#ApplicationName}; Filename: {app}\{#ApplicationName}.exe
