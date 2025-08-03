@echo off
echo Starting Docker deployment with embedded PostgreSQL...

echo.
echo 1. Checking if Docker is running...
docker --version
if %errorlevel% neq 0 (
    echo ERROR: Docker is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

echo.
echo 2. Checking for environment file...
if not exist ".env" (
    echo ERROR: .env file not found. Please ensure it exists.
    pause
    exit /b 1
)

echo.
echo 3. Creating data directory for PostgreSQL...
if not exist "data" mkdir data

echo.
echo 4. Building and starting services with embedded PostgreSQL...
echo    This will use the original docker-compose.yml with PostgreSQL container...
docker-compose -f docker/docker-compose.yml up --build -d

echo.
echo 5. Waiting for services to start...
timeout /t 20

echo.
echo 6. Checking service status...
docker-compose -f docker/docker-compose.yml ps

echo.
echo 7. Deployment with embedded PostgreSQL complete! Services available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo    - Backend API: http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - Swagger UI: http://localhost:8080/swagger
echo.
echo PostgreSQL database will be persisted in the './data' directory

echo.
echo To view logs: docker-compose -f docker/docker-compose.yml logs -f
echo To stop: docker-compose -f docker/docker-compose.yml down
echo.
pause