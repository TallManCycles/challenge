@echo off
setlocal enabledelayedexpansion

:: Deploy with versioned Docker images
:: Usage: deploy-versioned.bat [version]
:: Example: deploy-versioned.bat master-abc1234
:: Example: deploy-versioned.bat v1.0.0
:: Example: deploy-versioned.bat latest

:: Configuration
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%..\"
set "DOCKER_DIR=%PROJECT_DIR%docker"
set "COMPOSE_FILE=%DOCKER_DIR%\docker-compose.versioned.yml"
set "ENV_FILE=%DOCKER_DIR%\.env"

:: Default values
set "DEFAULT_VERSION=latest"
set "REPO_NAME=your-username/your-repo"

:: Parse arguments
set "VERSION=%DEFAULT_VERSION%"
set "SERVICES="
set "ACTION=deploy"

:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="-h" goto :show_help
if /i "%~1"=="--help" goto :show_help
if /i "%~1"=="-r" (
    set "REPO_NAME=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--repo" (
    set "REPO_NAME=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--backend-only" (
    set "SERVICES=backend postgres"
    shift
    goto :parse_args
)
if /i "%~1"=="--frontend-only" (
    set "SERVICES=frontend"
    shift
    goto :parse_args
)
if /i "%~1"=="--status" (
    set "ACTION=status"
    shift
    goto :parse_args
)
if /i "%~1"=="--logs" (
    set "ACTION=logs"
    shift
    goto :parse_args
)
if /i "%~1"=="--stop" (
    set "ACTION=stop"
    shift
    goto :parse_args
)
if "%~1"=="-*" (
    echo ❌ Unknown option: %~1
    goto :show_help
)
set "VERSION=%~1"
shift
goto :parse_args

:args_done

echo 🚀 Versioned Deployment Script
echo Repository: %REPO_NAME%

if "%ACTION%"=="status" goto :show_status
if "%ACTION%"=="logs" goto :show_logs
if "%ACTION%"=="stop" goto :stop_services

goto :check_prerequisites

:show_help
echo Usage: %~nx0 [OPTIONS] [VERSION]
echo.
echo Deploy the application using versioned Docker images from GitHub Container Registry
echo.
echo Arguments:
echo   VERSION          Docker image version to deploy ^(default: %DEFAULT_VERSION%^)
echo.
echo Options:
echo   -h, --help       Show this help message
echo   -r, --repo       Set repository name ^(default: %REPO_NAME%^)
echo   --backend-only   Deploy only the backend service
echo   --frontend-only  Deploy only the frontend service
echo   --status         Show deployment status
echo   --logs           Show service logs
echo   --stop           Stop all services
echo.
echo Examples:
echo   %~nx0 latest                    # Deploy latest version
echo   %~nx0 master-abc1234           # Deploy specific commit version
echo   %~nx0 v1.0.0                   # Deploy tagged release
echo   %~nx0 --backend-only latest    # Deploy only backend
echo   %~nx0 --status                 # Show current status
exit /b 0

:check_prerequisites
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not installed or not in PATH
    exit /b 1
)

docker compose version >nul 2>&1
if errorlevel 1 (
    docker-compose --version >nul 2>&1
    if errorlevel 1 (
        echo ❌ Docker Compose is not installed or not available
        exit /b 1
    )
    set "COMPOSE_CMD=docker-compose"
) else (
    set "COMPOSE_CMD=docker compose"
)

if not exist "%ENV_FILE%" (
    echo ❌ Environment file not found: %ENV_FILE%
    echo Please copy %DOCKER_DIR%\.env.example to %ENV_FILE% and configure it
    exit /b 1
)

echo ✅ Prerequisites check passed
goto :update_env_file

:update_env_file
set "BACKEND_IMAGE=ghcr.io/%REPO_NAME%/backend:%VERSION%"
set "FRONTEND_IMAGE=ghcr.io/%REPO_NAME%/frontend:%VERSION%"

echo 📝 Updating environment file with version: %VERSION%

:: Create backup
copy "%ENV_FILE%" "%ENV_FILE%.backup" >nul

:: Update environment file using PowerShell for reliable text processing
powershell -Command "& {
    $envFile = '%ENV_FILE%';
    $backendImage = '%BACKEND_IMAGE%';
    $frontendImage = '%FRONTEND_IMAGE%';
    
    $content = Get-Content $envFile;
    $newContent = @();
    $backendFound = $false;
    $frontendFound = $false;
    
    foreach ($line in $content) {
        if ($line -match '^BACKEND_IMAGE=') {
            $newContent += 'BACKEND_IMAGE=' + $backendImage;
            $backendFound = $true;
        } elseif ($line -match '^FRONTEND_IMAGE=') {
            $newContent += 'FRONTEND_IMAGE=' + $frontendImage;
            $frontendFound = $true;
        } else {
            $newContent += $line;
        }
    }
    
    if (-not $backendFound) {
        $newContent += 'BACKEND_IMAGE=' + $backendImage;
    }
    
    if (-not $frontendFound) {
        $newContent += 'FRONTEND_IMAGE=' + $frontendImage;
    }
    
    $newContent | Out-File -FilePath $envFile -Encoding UTF8;
}"

echo 📝 Backend image: %BACKEND_IMAGE%
echo 📝 Frontend image: %FRONTEND_IMAGE%
goto :pull_images

:pull_images
echo 🔄 Pulling Docker images for version: %VERSION%

docker pull "%BACKEND_IMAGE%"
if errorlevel 1 (
    echo ❌ Failed to pull backend image. Make sure the image exists and you're authenticated.
    echo Run: echo %%GITHUB_TOKEN%% ^| docker login ghcr.io -u USERNAME --password-stdin
    exit /b 1
)

docker pull "%FRONTEND_IMAGE%"
if errorlevel 1 (
    echo ❌ Failed to pull frontend image. Make sure the image exists and you're authenticated.
    exit /b 1
)

echo ✅ Images pulled successfully
goto :deploy

:deploy
echo 🚀 Deploying version: %VERSION%

cd /d "%DOCKER_DIR%"

if "%SERVICES%"=="" (
    echo 📦 Deploying all services
    %COMPOSE_CMD% -f docker-compose.versioned.yml up -d
) else (
    echo 📦 Deploying services: %SERVICES%
    %COMPOSE_CMD% -f docker-compose.versioned.yml up -d %SERVICES%
)

if errorlevel 1 (
    echo ❌ Deployment failed
    exit /b 1
)

echo ✅ Deployment completed
echo.
echo 📋 Service status:
%COMPOSE_CMD% -f docker-compose.versioned.yml ps
goto :end

:show_status
cd /d "%DOCKER_DIR%"

:: Determine compose command
docker compose version >nul 2>&1
if errorlevel 1 (
    set "COMPOSE_CMD=docker-compose"
) else (
    set "COMPOSE_CMD=docker compose"
)

echo 📋 Current deployment status:
%COMPOSE_CMD% -f docker-compose.versioned.yml ps
goto :end

:show_logs
cd /d "%DOCKER_DIR%"

:: Determine compose command
docker compose version >nul 2>&1
if errorlevel 1 (
    set "COMPOSE_CMD=docker-compose"
) else (
    set "COMPOSE_CMD=docker compose"
)

echo 📝 Service logs ^(press Ctrl+C to exit^):
%COMPOSE_CMD% -f docker-compose.versioned.yml logs -f
goto :end

:stop_services
cd /d "%DOCKER_DIR%"

:: Determine compose command
docker compose version >nul 2>&1
if errorlevel 1 (
    set "COMPOSE_CMD=docker-compose"
) else (
    set "COMPOSE_CMD=docker compose"
)

echo 🛑 Stopping all services...
%COMPOSE_CMD% -f docker-compose.versioned.yml down
echo ✅ All services stopped
goto :end

:end
endlocal