@echo off
echo Starting local Docker deployment (with external PostgreSQL)...

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
echo 3. Building and starting services with external PostgreSQL...
docker-compose -f docker/docker-compose.external-db.yml up --build -d

echo.
echo 4. Waiting for services to start...
timeout /t 15

echo.
echo 5. Checking service status...
docker-compose -f docker/docker-compose.external-db.yml ps

echo.
echo 6. Local deployment complete! Services available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo    - Backend API (ASP.NET Core): http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - API Documentation (Swagger): http://localhost:8080/swagger
echo.
echo NOTE: This deployment uses external PostgreSQL. Ensure your PostgreSQL database
echo       is accessible with the connection details specified in your .env file.

echo.
echo To view logs: docker-compose -f docker/docker-compose.external-db.yml logs -f
echo To stop: docker-compose -f docker/docker-compose.external-db.yml down
echo.
pause