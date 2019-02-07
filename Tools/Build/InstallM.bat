@echo off

set appDir=C:\VCalc\

if exist %appDir% @rd %appDir% /s /q >> log.txt
mkdir %appDir% >> log.txt
move "app\*" %appDir% >> log.txt
if not %ERRORLEVEL%==0 goto appCopyFailed 

call MReader.exe g >> log.txt

move "output.txt" "C:\Program Files\MOXA\USBDriver\v2.txt" >> log.txt
if not %ERRORLEVEL%==0 goto copyFailed 

call MReader c
if not %ERRORLEVEL%==0 goto checkFailed

del MReader.exe /s /f /q >> log.txt

echo Installation succeeded >> log.txt
@rd app /s /q >> log.txt
(goto) 2>nul & del "%~f0" >> log.txt
exit 0

:appCopyFailed
echo app copy failed >> log.txt
exit 2

:copyFailed
echo Mreader get failed >> log.txt
exit 1

:checkFailed
echo Mreader check failed >> log.txt
exit 3