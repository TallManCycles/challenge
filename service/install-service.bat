@echo off
setlocal enabledelayedexpansion

echo =================================================
echo    Fit File Monitor Service Installation
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

REM Get the directory where this script is located (should contain the .exe)
set "SERVICE_DIR=%~dp0"

REM Remove trailing backslash if present
if "%SERVICE_DIR:~-1%"=="\" set "SERVICE_DIR=%SERVICE_DIR:~0,-1%"

echo Service directory: %SERVICE_DIR%
echo Looking for: %SERVICE_DIR%\FitFileMonitorService.exe
echo.

REM Check if service files exist in the same directory as this script
if not exist "%SERVICE_DIR%\FitFileMonitorService.exe" (
    echo ERROR: FitFileMonitorService.exe not found in the current directory!
    echo This script should be placed in the same folder as FitFileMonitorService.exe
    echo Please make sure all service files are in the same directory as this script.
    echo.
    pause
    exit /b 1
)

echo Service executable found: %SERVICE_DIR%\FitFileMonitorService.exe
echo.

REM Set default values
set "DEFAULT_API_URL=http://localhost:5123"
set "DEFAULT_SECRET_KEY=my-secret-key (change this)"
set "DEFAULT_MONITOR_PATH=C:\Users\%USERNAME%\Documents\Zwift\Activities"
set "DEFAULT_INTERVAL=5"
set "DEFAULT_TRACKING_PATH=C:\ProgramData\FitFileMonitor\processed_files.txt"

echo =================================================
echo    Configuration Setup
echo =================================================
echo Please enter the configuration values.
echo Press ENTER to use the default value shown in brackets.
echo.

REM Prompt for API Base URL
set /p "API_URL=Backend API URL [%DEFAULT_API_URL%]: "
if "%API_URL%"=="" set "API_URL=%DEFAULT_API_URL%"

REM Prompt for Secret Key
set /p "SECRET_KEY=API Secret Key [%DEFAULT_SECRET_KEY%]: "
if "%SECRET_KEY%"=="" set "SECRET_KEY=%DEFAULT_SECRET_KEY%"

REM Prompt for Monitor Path
set /p "MONITOR_PATH=Directory to monitor [%DEFAULT_MONITOR_PATH%]: "
if "%MONITOR_PATH%"=="" set "MONITOR_PATH=%DEFAULT_MONITOR_PATH%"

REM Prompt for Check Interval
set /p "INTERVAL=Check interval in minutes [%DEFAULT_INTERVAL%]: "
if "%INTERVAL%"=="" set "INTERVAL=%DEFAULT_INTERVAL%"

REM Prompt for Tracking Path
set /p "TRACKING_PATH=Processed files tracking path [%DEFAULT_TRACKING_PATH%]: "
if "%TRACKING_PATH%"=="" set "TRACKING_PATH=%DEFAULT_TRACKING_PATH%"

echo.
echo =================================================
echo    Configuration Summary
echo =================================================
echo API URL: %API_URL%
echo Secret Key: %SECRET_KEY%
echo Monitor Path: %MONITOR_PATH%
echo Check Interval: %INTERVAL% minutes
echo Tracking Path: %TRACKING_PATH%
echo.

set /p "CONFIRM=Is this configuration correct? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo Installation cancelled.
    pause
    exit /b 1
)

echo.
echo =================================================
echo    Creating Configuration File
echo =================================================

REM Escape backslashes for JSON
set "MONITOR_PATH_JSON=!MONITOR_PATH:\=\\!"
set "TRACKING_PATH_JSON=!TRACKING_PATH:\=\\!"

REM Create appsettings.json
echo Creating %SERVICE_DIR%\appsettings.json...
(
echo {
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information",
echo       "Microsoft.Hosting.Lifetime": "Information"
echo     }
echo   },
echo   "FitFileMonitor": {
echo     "ApiBaseUrl": "%API_URL%",
echo     "ApiSecretKey": "%SECRET_KEY%",
echo     "MonitorPath": "%MONITOR_PATH_JSON%",
echo     "CheckIntervalMinutes": %INTERVAL%,
echo     "ProcessedFilesTrackingPath": "%TRACKING_PATH_JSON%"
echo   }
echo }
) > "%SERVICE_DIR%\appsettings.json"

echo Configuration file created successfully!
echo.

REM Create the tracking directory if it doesn't exist
for %%F in ("%TRACKING_PATH%") do set "TRACKING_DIR=%%~dpF"
if not exist "%TRACKING_DIR%" (
    echo Creating tracking directory: %TRACKING_DIR%
    mkdir "%TRACKING_DIR%" 2>nul
)

REM Create the monitor directory if it doesn't exist
if not exist "%MONITOR_PATH%" (
    echo Creating monitor directory: %MONITOR_PATH%
    mkdir "%MONITOR_PATH%" 2>nul
)

echo =================================================
echo    Installing Windows Service
echo =================================================

REM Stop and remove existing service if it exists
sc query FitFileMonitorService >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopping existing service...
    sc stop FitFileMonitorService >nul 2>&1
    timeout /t 3 /nobreak >nul
    echo Removing existing service...
    sc delete FitFileMonitorService >nul 2>&1
    timeout /t 2 /nobreak >nul
)

REM Install the service
echo Installing service...
sc create FitFileMonitorService binPath="%SERVICE_DIR%\FitFileMonitorService.exe" start=auto DisplayName="Fit File Monitor Service" depend=Tcpip

if %errorlevel% neq 0 (
    echo ERROR: Failed to install service!
    pause
    exit /b 1
)

echo Service installed successfully!

REM Set service description
sc description FitFileMonitorService "Monitors directory for .fit files and uploads them to backend API"

echo.
echo =================================================
echo    Starting Service
echo =================================================

echo Starting the service...
sc start FitFileMonitorService

if %errorlevel% neq 0 (
    echo WARNING: Service was installed but failed to start.
    echo Check the Event Viewer for error details.
    echo You can manually start it from Services.msc
) else (
    echo Service started successfully!
)

echo.
echo =================================================
echo    Installation Complete
echo =================================================
echo.
echo Service Name: FitFileMonitorService
echo Display Name: Fit File Monitor Service
echo Service Path: %SERVICE_DIR%\FitFileMonitorService.exe
echo Config File: %SERVICE_DIR%\appsettings.json
echo.
echo You can manage the service using:
echo - Services.msc (Windows Services Manager)
echo - sc stop FitFileMonitorService
echo - sc start FitFileMonitorService
echo.
echo Logs are available in Windows Event Viewer under:
echo Applications and Services Logs ^> FitFileMonitorService
echo.
echo To uninstall the service, run: uninstall-service.bat
echo.
pause