@echo off
setlocal enabledelayedexpansion

:: Create and push a version tag
:: Usage: create-version.bat [version]
:: Example: create-version.bat 1.1.0
:: Example: create-version.bat 1.2.0

set "VERSION=%~1"

if "%VERSION%"=="" (
    echo ❌ Version is required
    echo Usage: %~nx0 ^<version^>
    echo Example: %~nx0 1.1.0
    exit /b 1
)

:: Basic version format validation
echo %VERSION% | findstr /r "^[0-9]*\.[0-9]*\(\.[0-9]*\)\?$" >nul
if errorlevel 1 (
    echo ❌ Invalid version format. Use semantic versioning like 1.0.0 or 1.1
    exit /b 1
)

:: Check if tag already exists
git tag --list | findstr /r "^v%VERSION%$" >nul
if not errorlevel 1 (
    echo ❌ Tag v%VERSION% already exists
    exit /b 1
)

echo 🏷️  Creating version tag: v%VERSION%

:: Create annotated tag
git tag -a "v%VERSION%" -m "Release version %VERSION%"

if errorlevel 1 (
    echo ❌ Failed to create tag
    exit /b 1
)

echo ✅ Tag v%VERSION% created locally
echo 🚀 Pushing tag to trigger Docker image build...

:: Push the tag
git push origin "v%VERSION%"

if errorlevel 1 (
    echo ❌ Failed to push tag
    exit /b 1
)

echo ✅ Tag v%VERSION% pushed to GitHub
echo 📦 GitHub Actions will now build and push Docker images for version %VERSION%

:: Get repository URL for actions link
for /f "tokens=*" %%i in ('git config --get remote.origin.url') do set REPO_URL=%%i

:: Handle both SSH and HTTPS URLs
echo %REPO_URL% | findstr "^git@" >nul
if not errorlevel 1 (
    :: SSH URL: git@github.com:user/repo.git
    set REPO_URL=%REPO_URL:git@github.com:=%
) else (
    :: HTTPS URL: https://github.com/user/repo.git
    set REPO_URL=%REPO_URL:https://github.com/=%
)
:: Remove .git suffix if present
set REPO_URL=%REPO_URL:.git=%
echo 🔗 Check the build status at: https://github.com/%REPO_URL%/actions

echo.
echo 📋 Once the build completes, you can deploy with:
echo   deploy-version.bat %VERSION%
echo   ./deploy-version.sh %VERSION%

endlocal