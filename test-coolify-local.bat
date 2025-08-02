@echo off
echo Testing Coolify Docker Compose locally...

echo.
echo 1. Checking if Docker is running...
docker --version
if %errorlevel% neq 0 (
    echo ERROR: Docker is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

echo.
echo 2. Copying test environment file...
copy .env.coolify-test .env
if %errorlevel% neq 0 (
    echo ERROR: Could not copy .env.coolify-test to .env
    pause
    exit /b 1
)

echo.
echo 3. Building and starting services with Coolify test compose...
docker-compose -f docker/docker-compose.coolify-test.yml up --build -d

echo.
echo 4. Waiting for services to start...
timeout /t 30

echo.
echo 5. Checking service status...
docker-compose -f docker/docker-compose.coolify-test.yml ps

echo.
echo 6. Testing service endpoints...
echo Testing Backend Health Check:
curl -s http://localhost:8080/api/health || echo "Backend not ready yet"
echo.
echo Testing Frontend:
curl -s -o nul -w "%%{http_code}" http://localhost || echo "Frontend not ready yet"
echo.

echo.
echo 7. Coolify test deployment complete! Services available at:
echo    - Frontend: http://localhost (port 80)
echo    - Backend API: http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - Swagger UI: http://localhost:8080/swagger
echo    - PostgreSQL: localhost:5432
echo.
echo Database will be persisted in Docker volume 'postgres_data'

echo.
echo Useful commands:
echo   View all logs:     docker-compose -f docker/docker-compose.coolify-test.yml logs -f
echo   View backend logs: docker-compose -f docker/docker-compose.coolify-test.yml logs -f backend
echo   Stop all services: docker-compose -f docker/docker-compose.coolify-test.yml down
echo   Stop and remove volumes: docker-compose -f docker/docker-compose.coolify-test.yml down -v
echo.
echo Press any key to continue or Ctrl+C to exit...
pause