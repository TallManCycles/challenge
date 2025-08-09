@echo off
setlocal enabledelayedexpansion

:: Quick deployment script for versioned Docker images
:: Usage: deploy-version.bat [version]
:: Example: deploy-version.bat 1.0
:: Example: deploy-version.bat master-abc1234
:: Example: deploy-version.bat 1.2.0

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=1.0"

set "DOCKER_DIR=%~dp0docker"
set "ENV_FILE=%DOCKER_DIR%\.env"

echo 🚀 Deploying version: %VERSION%

:: Check if .env file exists
if not exist "%ENV_FILE%" (
    echo ❌ Environment file not found: %ENV_FILE%
    echo Please copy %DOCKER_DIR%\.env.example to %ENV_FILE% and configure it
    exit /b 1
)

:: Update .env file with new image versions
echo 📝 Updating image versions...

:: Use PowerShell to update the environment file
powershell -Command "& {
    $envFile = '%ENV_FILE%';
    $version = '%VERSION%';
    
    $content = Get-Content $envFile;
    $newContent = @();
    
    foreach ($line in $content) {
        if ($line -match '^BACKEND_IMAGE=') {
            $newContent += 'BACKEND_IMAGE=ghcr.io/tallmancycles/challenge/backend:' + $version;
        } elseif ($line -match '^FRONTEND_IMAGE=') {
            $newContent += 'FRONTEND_IMAGE=ghcr.io/tallmancycles/challenge/frontend:' + $version;
        } else {
            $newContent += $line;
        }
    }
    
    $newContent | Out-File -FilePath $envFile -Encoding UTF8;
    Write-Host '✅ Environment file updated';
}"

:: Pull the images
echo 🔄 Pulling Docker images...
docker pull ghcr.io/tallmancycles/challenge/backend:%VERSION%
docker pull ghcr.io/tallmancycles/challenge/frontend:%VERSION%

if errorlevel 1 (
    echo ❌ Failed to pull images. Make sure you're authenticated with GitHub Container Registry:
    echo echo %%GITHUB_TOKEN%% ^| docker login ghcr.io -u your-username --password-stdin
    exit /b 1
)

:: Deploy using docker-compose
echo 📦 Deploying services...
cd /d "%DOCKER_DIR%"
docker-compose -f docker-compose.coolify.yml up -d

if errorlevel 1 (
    echo ❌ Deployment failed
    exit /b 1
)

echo ✅ Deployment completed successfully!
echo.
echo 📋 Service status:
docker-compose -f docker-compose.coolify.yml ps

echo.
echo 🌐 Your application should be available at:
echo   Frontend: http://localhost (port from FRONTEND_PORT)
echo   Backend:  http://localhost:8080 (port from BACKEND_PORT)
echo.
echo 💡 To deploy a different version:
echo   deploy-version.bat 1.1.0      (deploy version 1.1.0)
echo   deploy-version.bat master-abc123  (deploy specific commit)

endlocal