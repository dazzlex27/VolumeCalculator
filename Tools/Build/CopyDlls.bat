@echo off

if exist "%outputFolder%" @rd "%outputFolder%" /s /q
xcopy "%binariesFolder%\Confused\*" "%outputFolder%\*" /s /y
xcopy "%binariesFolder%\libD435FrameProvider.dll" "%outputFolder%\" /s /y
xcopy "%binariesFolder%\libDepthMapProcessor.dll" "%outputFolder%\" /s /y
xcopy "%binariesFolder%\Microsoft.Kinect.dll" "%outputFolder%\" /s /y
xcopy "%binariesFolder%\opencv_world310.dll" "%outputFolder%\" /s /y
xcopy "%binariesFolder%\realsense2.dll" "%outputFolder%\" /s /y
xcopy "%binariesFolder%\realsense2.dll" "%outputFolder%\" /s /y
xcopy "%rootFolder%\Primitives\settings.cfg" "%outputFolder%\" /s /y