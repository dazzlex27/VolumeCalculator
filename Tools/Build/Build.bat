@echo off

set rootFolder=..\..
set devenvPath=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE
set inputFolder=%rootFolder%\!!bin
set binariesFolder=%inputFolder%\x64\Release
set buildFolder=%inputFolder%\Installers

if exist logs @rd logs /s /q
mkdir logs

echo Removing output folders...
if exist %binariesFolder% @rd %binariesFolder% /s /q

if not "%~1"=="pro" goto main
echo Switching to Pro...
call "%devenvPath%\devenv.exe" %rootFolder%\VolumeCalculator.sln /Build "Release|x64" /Project "Utils\ProBuilder\ProBuilder.csproj" /Out "logs\version-build.log"
if not %ERRORLEVEL%==0 goto failed
call %binariesfolder%\ProBuilder.exe s
if not %ERRORLEVEL%==0 goto failed

:main

echo Building solution...
call "%devenvPath%\devenv.exe" %rootFolder%\VolumeCalculator.sln /Build "Release|x64" /Out "logs\vcbuild.log"
if not %ERRORLEVEL%==0 goto failed

echo Writing version...
call "%binariesFolder%\VersionWriter.exe"
if not %ERRORLEVEL%==0 goto failed

set /p appversion=<"%binariesFolder%\appversion.txt"
set outputFolder=%buildFolder%\%appversion%
echo %appversion% > logs\version.log

echo Obfuscating...
call ..\ConfuserEx\Confuser.CLI.exe vcalc.crproj > logs\confuser.log
if not %ERRORLEVEL%==0 goto failed

echo Building installer...
call "C:\Program Files (x86)\Inno Setup 5\iscc.exe" install.iss > logs\innosetup.log
if not %ERRORLEVEL%==0 goto failed

if exist "%outputFolder%" @rd "%outputFolder%" /s /q

if not "%~1"=="pro" goto main2
echo Restoring edition...
call %binariesfolder%\ProBuilder.exe r
if not %ERRORLEVEL%==0 goto failed

:main2

echo Build succeeded!
exit 0

:failed 
echo Build failed!
exit 1