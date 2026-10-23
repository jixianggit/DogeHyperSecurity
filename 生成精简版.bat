@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo    DogeHyperSecurity Lite Build
echo ============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet not found. Please install .NET 9 SDK.
    goto :fail
)

if not exist "Native\QPBridge\bin\Release\QPBridge.dll" (
    echo [ERROR] Missing Native\QPBridge\bin\Release\QPBridge.dll
    goto :fail
)

if not exist "Payload\*.exe" (
    echo [ERROR] No exe found in Payload folder.
    goto :fail
)

echo Building single-file lite package...
dotnet publish DogeHyperSecurity.csproj -p:PublishProfile=win-x64-lite -v q
if errorlevel 1 (
    echo [ERROR] Publish failed. Make sure the app is not running.
    goto :fail
)

echo.
echo [DONE] publish\DogeHyperSecurity.exe
echo.
if not defined NO_PAUSE start "" "%~dp0publish"
if not defined NO_PAUSE pause
exit /b 0

:fail
echo.
if not defined NO_PAUSE pause
exit /b 1
