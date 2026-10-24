@echo off
setlocal
pushd "%~dp0"
set "GAME_EXE=%CD%\Builds\Windows\SunsetState.exe"
if not exist "%GAME_EXE%" goto missing
if not exist "%CD%\Builds\Windows\UnityPlayer.dll" goto missing
if not exist "%CD%\Builds\Windows\SunsetState_Data\globalgamemanagers" goto missing
if "%~1"=="--check" goto ready
if "%~1"=="--smoke" goto smoke
if "%~1"=="--windowed" goto windowed
echo Starting Sunset State. F11 switches window mode.
start "Sunset State" /d "%CD%\Builds\Windows" "%GAME_EXE%" -logFile "%CD%\game.log"
exit /b 0
:windowed
start "Sunset State" /d "%CD%\Builds\Windows" "%GAME_EXE%" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "%CD%\game.log"
exit /b 0
:smoke
start "" /wait "%GAME_EXE%" -batchmode -nographics -sunset-smoke -logFile "%CD%\runtime-check.log"
exit /b %errorlevel%
:ready
echo GAME CHECK OK
exit /b 0
:missing
echo Game files are missing. Run BuildAndRun.bat first.
echo Keep SunsetState.exe, UnityPlayer.dll and SunsetState_Data together.
if not "%~1"=="--check" pause
exit /b 1
