@echo off

echo %binariesFolder%

set appFolder=%outputFolder%\app\
set webFolder=%outputFolder%\web\

if exist "%outputFolder%" @rd "%outputFolder%" /s /q
if exist "%appFolder%" @rd "%appFolder%" /s /q
if exist "%webFolder%" @rd "%webFolder%" /s /q
xcopy "%binariesFolder%\Microsoft.Kinect.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\WinSCP.exe" "%appFolder%" /s /y
xcopy "%binariesFolder%\WinSCPnet.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\LibUsbDotNet.LibUsbDotNet.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\Fleck.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\Confused\*" "%appFolder%*" /s /y /e
xcopy "%binariesFolder%\VCConfigurator.exe" "%appFolder%" /s /y
xcopy "%binariesFolder%\libD435FrameProvider.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\libDepthMapProcessor.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\opencv_world310.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\realsense2.dll" "%appFolder%" /s /y
xcopy "%binariesFolder%\MReader.exe" "%outputFolder%\" /s /y
xcopy InstallM.bat "%outputFolder%" /s /y
xcopy "\..\..\web\*" "%webFolder%" /s /y /e

xcopy settings_standard.cfg "%appFolder%\settings.cfg*" /s /y
if "%~1"=="pro" xcopy settings_pro.cfg "%appFolder%\settings.cfg*" /s /y