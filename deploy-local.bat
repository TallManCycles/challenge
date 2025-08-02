@echo off
echo Starting local Docker deployment...

echo.
echo 1. Checking if Docker is running...
docker --version
if %errorlevel% neq 0 (
    echo ERROR: Docker is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

echo.
echo 2. Creating data directory...
if not exist "data" mkdir data

echo.
echo 3. Checking for environment file...
if not exist ".env" (
    echo ERROR: .env file not found. Please ensure it exists.
    pause
    exit /b 1
)

echo.
echo 4. Building and starting services with Docker Compose...
docker-compose up --build -d

echo.
echo 5. Waiting for services to start...
timeout /t 10

echo.
echo 6. Checking service status...
docker-compose ps

echo.
echo 7. Full stack deployment complete! Services available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo    - Backend API (ASP.NET Core): http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - API Documentation (Swagger): http://localhost:8080/swagger
echo.
echo Database will be persisted in the './data' directory

echo.
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down
echo.
pause