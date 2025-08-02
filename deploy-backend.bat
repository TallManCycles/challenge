@echo off
echo Starting Backend Docker deployment...

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
echo 3. Creating data directory...
if not exist "data" mkdir data

echo.
echo 4. Creating shared network (if it doesn't exist)...
docker network create challenge-network 2>nul || echo Network already exists

echo.
echo 5. Building and starting backend service...
docker-compose -f docker-compose.backend.yml -p challenge-backend up --build -d

echo.
echo 6. Waiting for backend to start...
timeout /t 15

echo.
echo 7. Checking backend status...
docker-compose -f docker-compose.backend.yml -p challenge-backend ps

echo.
echo 8. Backend deployment complete! Service available at:
echo    - Backend API: http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - Swagger UI: http://localhost:8080/swagger
echo.
echo Database will be persisted in the './data' directory

echo.
echo To view logs: docker-compose -f docker-compose.backend.yml -p challenge-backend logs -f
echo To stop: docker-compose -f docker-compose.backend.yml -p challenge-backend down
echo.
pause