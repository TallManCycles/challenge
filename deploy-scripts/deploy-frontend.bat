@echo off
echo Starting Frontend Docker deployment...

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
echo 3. Building and starting frontend service...
docker-compose -f docker/docker-compose.frontend.yml -p challenge-frontend up --build -d

echo.
echo 4. Waiting for frontend to start...
timeout /t 10

echo.
echo 5. Checking frontend status...
docker-compose -f docker/docker-compose.frontend.yml -p challenge-frontend ps

echo.
echo 6. Frontend deployment complete! Service available at:
echo    - Frontend (Vue.js): http://localhost:3000
echo.
echo NOTE: Make sure backend is running for full functionality!
echo You can start backend with: .\deploy-backend.bat

echo.
echo To view logs: docker-compose -f docker/docker-compose.frontend.yml -p challenge-frontend logs -f
echo To stop: docker-compose -f docker/docker-compose.frontend.yml -p challenge-frontend down
echo.
pause