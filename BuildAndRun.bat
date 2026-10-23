@echo off
setlocal
pushd "%~dp0"
call "%~dp0Build.bat"
if errorlevel 1 goto failed
call "%~dp0Play.bat" %*
exit /b %errorlevel%
:failed
echo.
echo Build failed. Read unity-build.log for details.
echo Close this project in Unity Editor before building from this file.
pause
exit /b 1
