@echo off
setlocal
pushd "%~dp0"
set "UNITY_VERSION=6000.0.77f1"
for /f "tokens=2" %%V in ('findstr /b /c:"m_EditorVersion:" "ProjectSettings\ProjectVersion.txt"') do set "UNITY_VERSION=%%V"
set "UNITY_EXE=%UNITY_EDITOR%"
if not defined UNITY_EXE set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" goto missing
if "%~1"=="--check" goto ready
echo Building Sunset State. This can take several minutes.
echo Editor: "%UNITY_EXE%"
echo Build log: "%~dp0unity-build.log"
start "" /wait "%UNITY_EXE%" -batchmode -nographics -projectPath "%CD%" -executeMethod SunsetState.Editor.ProjectSetup.BuildWindows -quit -logFile "%CD%\unity-build.log"
if errorlevel 1 exit /b 1
findstr /c:"SUNSET_BUILD_OK" "unity-build.log" >nul
if errorlevel 1 exit /b 1
if not exist "Builds\Windows\SunsetState.exe" exit /b 1
copy /y README.md "Builds\Windows\README.md" >nul
copy /y THIRD_PARTY_NOTICES.md "Builds\Windows\THIRD_PARTY_NOTICES.md" >nul
if exist VALIDATION.md copy /y VALIDATION.md "Builds\Windows\VALIDATION.md" >nul
if exist Previews xcopy Previews "Builds\Windows\Previews\" /I /Y /Q >nul
echo BUILD OK
exit /b 0
:ready
echo EDITOR CHECK OK
exit /b 0
:missing
echo Unity %UNITY_VERSION% was not found at:
echo "%UNITY_EXE%"
echo Install this version using Unity Hub, including Windows Build Support.
echo You can also set UNITY_EDITOR to the full path to Unity.exe.
exit /b 1
