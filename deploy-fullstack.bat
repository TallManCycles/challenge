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
echo 3. Creating data directory...
if not exist "data" mkdir data

echo.
echo 4. Creating shared network (if it doesn't exist)...
docker network create challenge-network 2>nul || echo Network already exists

echo.
echo 5. Building and starting backend service...
docker-compose -f docker-compose.backend.yml -p challenge-backend up --build -d

echo.
echo 6. Waiting for backend to initialize...
timeout /t 15

echo.
echo 7. Building and starting frontend service...
docker-compose -f docker-compose.frontend.yml -p challenge-frontend up --build -d

echo.
echo 8. Waiting for frontend to start...
timeout /t 10

echo.
echo 9. Checking service status...
echo Backend:
docker-compose -f docker-compose.backend.yml -p challenge-backend ps
echo.
echo Frontend:
docker-compose -f docker-compose.frontend.yml -p challenge-frontend ps

echo.
echo 10. Full stack deployment complete! Services available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo    - Backend API: http://localhost:8080
echo    - Health Check: http://localhost:8080/api/health
echo    - Swagger UI: http://localhost:8080/swagger
echo.
echo Database will be persisted in the './data' directory

echo.
echo Useful commands:
echo   View backend logs:  docker-compose -f docker-compose.backend.yml -p challenge-backend logs -f
echo   View frontend logs: docker-compose -f docker-compose.frontend.yml -p challenge-frontend logs -f
echo   Stop backend:       docker-compose -f docker-compose.backend.yml -p challenge-backend down
echo   Stop frontend:      docker-compose -f docker-compose.frontend.yml -p challenge-frontend down
echo   Stop both:          docker-compose -f docker-compose.backend.yml -p challenge-backend down ^&^& docker-compose -f docker-compose.frontend.yml -p challenge-frontend down
echo.
pause