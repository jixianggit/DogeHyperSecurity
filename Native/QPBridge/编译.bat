@echo off
setlocal
cd /d "%~dp0"

set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
)

if not defined MSBUILD (
    echo [ERROR] MSBuild not found. Install Visual Studio 2022 with C++ tools.
    goto :fail
)

echo ============================================
echo    Build QPBridge.dll (Release x64)
echo ============================================
echo.

"%MSBUILD%" "QPBridge.vcxproj" /p:Configuration=Release /p:Platform=x64 /m /v:q /nologo
if errorlevel 1 goto :fail

echo.
echo [DONE] bin\Release\QPBridge.dll
echo.
if not defined NO_PAUSE start "" "%~dp0bin\Release"
if not defined NO_PAUSE pause
exit /b 0

:fail
echo.
echo [FAILED] Build error.
if not defined NO_PAUSE pause
exit /b 1
