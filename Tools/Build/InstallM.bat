@echo off

call MReader.exe g

move "output.txt" "C:\Program Files\MOXA\USBDriver\v2.txt"
if not %ERRORLEVEL%==0 goto copyFailed 

call MReader c
if not %ERRORLEVEL%==0 goto checkFailed

echo Mreader succeeded
exit 0

:copyFailed
echo Mreader failed
exit 1

:checkFailed
echo Mreader check failed
exit 3