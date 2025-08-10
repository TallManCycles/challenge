# Fit File Monitor Windows Service

This Windows service monitors a specified directory for new .fit files and automatically uploads them to your backend API.

## Features

- Monitors directory every 5 minutes (configurable)
- Tracks processed files to avoid duplicate uploads
- Secure API authentication with secret key
- Windows Service integration with proper logging
- Configurable paths and settings

## Configuration

The service uses `appsettings.json` for configuration:

```json
{
  "FitFileMonitor": {
    "ApiBaseUrl": "http://localhost:5000",
    "ApiSecretKey": "your-secret-key-here",
    "MonitorPath": "C:\\Users\\{username}\\Documents\\Zwift\\Activities",
    "CheckIntervalMinutes": 5,
    "ProcessedFilesTrackingPath": "C:\\ProgramData\\FitFileMonitor\\processed_files.txt"
  }
}
```

### Configuration Options

- `ApiBaseUrl`: The base URL of your backend API
- `ApiSecretKey`: Secret key for API authentication (must match backend configuration)
- `MonitorPath`: Directory to monitor for .fit files. Use `{username}` as placeholder for current user
- `CheckIntervalMinutes`: How often to check for new files (in minutes)
- `ProcessedFilesTrackingPath`: Path to file that tracks already processed files

## Quick Installation (Recommended)

For easy installation, use the provided batch scripts:

### 1. Build the Service
```cmd
build-service.bat
```

### 2. Install the Service (Run as Administrator)
```cmd
install-service.bat
```
This script will:
- Prompt you for all configuration settings
- Create the `appsettings.json` file with your settings
- Install and start the Windows service
- Create necessary directories

### 3. Uninstall the Service (Run as Administrator)
```cmd
uninstall-service.bat
```

## Manual Installation

### Building

```bash
cd service/FitFileMonitorService
dotnet build -c Release
```

### Publishing as Executable

To create a standalone executable:

```bash
cd service/FitFileMonitorService
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

This creates `FitFileMonitorService.exe` in the `publish` folder.

### Installing as Windows Service Manually

1. Build the service as shown above
2. Copy the published files to your desired location (e.g., `C:\Services\FitFileMonitor\`)
3. Update the `appsettings.json` with your configuration
4. Install using PowerShell as Administrator:

```powershell
New-Service -Name "FitFileMonitorService" -BinaryPathName "C:\Services\FitFileMonitor\FitFileMonitorService.exe" -Description "Monitors and uploads .fit files to backend API"
Start-Service -Name "FitFileMonitorService"
```

### Uninstalling Windows Service Manually

```powershell
Stop-Service -Name "FitFileMonitorService"
Remove-Service -Name "FitFileMonitorService"
```

## Backend API Configuration

Make sure your backend API is configured with the matching secret key in `appsettings.json`:

```json
{
  "FitFileUpload": {
    "SecretKey": "your-secret-key-here",
    "StoragePath": "/app/uploads/fit-files"
  }
}
```

## Monitoring and Logs

The service logs to multiple locations:

1. **Local File**: `log.txt` in the same directory as the service executable
   - Contains detailed logs with timestamps
   - Most convenient for troubleshooting
   - Format: `[2025-01-10 14:30:15] [INFO] FitFileMonitorService.Worker: Message`

2. **Windows Event Log**: Event Viewer under Applications and Services Logs > FitFileMonitorService
   - Standard Windows service logging
   - Integrated with Windows system logs

3. **Console Output**: When running in console mode (for testing)

## Default Paths

- **Zwift Activities**: `C:\Users\{username}\Documents\Zwift\Activities`
- **Processed Files Tracking**: `C:\ProgramData\FitFileMonitor\processed_files.txt`

## API Endpoints

The service uploads files to:
- `POST /api/fitfiles/upload` - Upload .fit file
- `GET /api/fitfiles/list` - List uploaded files (for debugging)

Both endpoints require `X-API-Secret` header with the configured secret key.