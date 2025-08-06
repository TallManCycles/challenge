@echo off
echo Starting Full Stack Docker deployment...

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
echo 4. Building and starting all services with Coolify configuration...
docker-compose -f docker/docker-compose.coolify.yml up --build -d

echo.
echo 5. Waiting for services to start...
timeout /t 20

echo.
echo 6. Checking service status...
docker-compose -f docker/docker-compose.coolify.yml ps

echo.
echo 7. Full stack deployment complete! Services available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo    - Backend API: http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - Swagger UI: http://localhost:8080/swagger
echo    - PostgreSQL Database: localhost:5432
echo.
echo Database will be persisted in the postgres_data volume

echo.
echo Useful commands:
echo   View all logs:        docker-compose -f docker/docker-compose.coolify.yml logs -f
echo   View backend logs:    docker-compose -f docker/docker-compose.coolify.yml logs -f backend
echo   View frontend logs:   docker-compose -f docker/docker-compose.coolify.yml logs -f frontend
echo   View postgres logs:   docker-compose -f docker/docker-compose.coolify.yml logs -f postgres
echo   Stop all services:    docker-compose -f docker/docker-compose.coolify.yml down
echo   Stop with volumes:    docker-compose -f docker/docker-compose.coolify.yml down -v
echo.
pause