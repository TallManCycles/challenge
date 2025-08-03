@echo off
echo Starting PostgreSQL Docker deployment...

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
echo 4. Starting PostgreSQL database...
docker-compose -f docker/docker-compose.postgres.yml -p challenge-postgres up -d

echo.
echo 5. Waiting for PostgreSQL to initialize...
timeout /t 15

echo.
echo 6. Checking PostgreSQL status...
docker-compose -f docker/docker-compose.postgres.yml -p challenge-postgres ps

echo.
echo 7. PostgreSQL deployment complete! Database available at:
echo    - PostgreSQL: localhost:5432
echo    - Database: garmin_challenge
echo    - Username: postgres
echo    - Password: postgres (or from your .env file)
echo.
echo Database will be persisted in the './data' directory

echo.
echo To view logs: docker-compose -f docker/docker-compose.postgres.yml -p challenge-postgres logs -f
echo To stop: docker-compose -f docker/docker-compose.postgres.yml -p challenge-postgres down
echo.
pause