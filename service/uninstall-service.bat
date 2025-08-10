@echo off
setlocal

echo =================================================
echo    Fit File Monitor Service Uninstallation
echo =================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Please right-click and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Check if service exists
sc query FitFileMonitorService >nul 2>&1
if %errorlevel% neq 0 (
    echo Service 'FitFileMonitorService' is not installed.
    echo Nothing to uninstall.
    echo.
    pause
    exit /b 0
)

echo Found service: FitFileMonitorService
echo.

set /p "CONFIRM=Are you sure you want to uninstall the Fit File Monitor Service? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo Uninstallation cancelled.
    pause
    exit /b 1
)

echo.
echo Stopping service...
sc stop FitFileMonitorService >nul 2>&1

REM Wait for service to stop
timeout /t 5 /nobreak >nul

echo Removing service...
sc delete FitFileMonitorService

if %errorlevel% neq 0 (
    echo ERROR: Failed to remove service!
    echo The service may still be running. Try stopping it manually first.
    pause
    exit /b 1
)

echo.
echo =================================================
echo    Uninstallation Complete
echo =================================================
echo.
echo The Fit File Monitor Service has been removed.
echo.
echo Note: Configuration files and processed files tracking
echo remain in place. You can delete them manually if desired:
echo - Service directory files
echo - C:\ProgramData\FitFileMonitor\processed_files.txt
echo.
pause