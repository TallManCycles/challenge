@echo off
setlocal

echo =================================================
echo    Fit File Monitor Service Build Script
echo =================================================
echo.

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%"
set "OUTPUT_DIR=%SCRIPT_DIR%publish"

REM Remove trailing backslash if present
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"

echo Script directory: %SCRIPT_DIR%
echo Project directory: %PROJECT_DIR%
echo Output directory: %OUTPUT_DIR%
echo.

REM Check if project exists
if not exist "%PROJECT_DIR%\FitFileMonitorService.csproj" (
    echo ERROR: Project file not found at %PROJECT_DIR%\FitFileMonitorService.csproj
    echo Make sure you're running this script from the service directory.
    pause
    exit /b 1
)

echo Cleaning previous build...
if exist "%OUTPUT_DIR%" (
    del /q "%OUTPUT_DIR%\*.exe" 2>nul
    del /q "%OUTPUT_DIR%\*.dll" 2>nul
    del /q "%OUTPUT_DIR%\*.json" 2>nul
    del /q "%OUTPUT_DIR%\*.pdb" 2>nul
)

echo.
echo Building and publishing service...
cd /d "%PROJECT_DIR%"

REM Publish the service as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -o "%OUTPUT_DIR%" --property:PublishSingleFile=false

if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Copying installation scripts...
copy "%SCRIPT_DIR%install-service.bat" "%OUTPUT_DIR%\" >nul 2>&1
copy "%SCRIPT_DIR%uninstall-service.bat" "%OUTPUT_DIR%\" >nul 2>&1

if exist "%OUTPUT_DIR%\install-service.bat" (
    echo - install-service.bat copied
) else (
    echo WARNING: Could not copy install-service.bat
)

if exist "%OUTPUT_DIR%\uninstall-service.bat" (
    echo - uninstall-service.bat copied  
) else (
    echo WARNING: Could not copy uninstall-service.bat
)

echo.
echo =================================================
echo    Build Complete
echo =================================================
echo.
echo Service files have been published to: %OUTPUT_DIR%
echo.
echo Files created in %OUTPUT_DIR%:
dir /b "%OUTPUT_DIR%\*.exe" 2>nul
dir /b "%OUTPUT_DIR%\*.bat" 2>nul
echo.
echo =================================================
echo    Distribution Package Ready
echo =================================================
echo.
echo The complete service package is ready in: %OUTPUT_DIR%
echo.
echo To distribute to end users:
echo 1. Copy the entire contents of the above directory 
echo 2. End user runs 'install-service.bat' as Administrator
echo 3. The script will guide them through configuration
echo.
echo All files needed are now in one folder - ready for distribution!
echo.
pause