# Version Management Script
# Usage: .\update-version.ps1 [major|minor|patch] or .\update-version.ps1 set [version]

param(
    [Parameter(Position=0)]
    [string]$Action = "patch",
    
    [Parameter(Position=1)]
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# File paths
$versionFile = "VERSION"
$backendCsproj = "backend\backend.csproj"
$frontendPackageJson = "frontend\package.json"
$dockerCompose = "docker\docker-compose.coolify.yml"

function Get-CurrentVersion {
    if (Test-Path $versionFile) {
        return (Get-Content $versionFile).Trim()
    }
    return "1.0.0"
}

function Set-Version {
    param([string]$newVersion)
    
    Write-Host "Setting version to: $newVersion" -ForegroundColor Green
    
    # Update VERSION file
    $newVersion | Out-File -FilePath $versionFile -Encoding UTF8 -NoNewline
    
    # Update backend .csproj
    if (Test-Path $backendCsproj) {
        $csprojContent = Get-Content $backendCsproj -Raw
        $csprojContent = $csprojContent -replace '<Version>[\d\.]+</Version>', "<Version>$newVersion</Version>"
        $csprojContent = $csprojContent -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$newVersion</AssemblyVersion>"
        $csprojContent = $csprojContent -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$newVersion</FileVersion>"
        $csprojContent | Out-File -FilePath $backendCsproj -Encoding UTF8 -NoNewline
        Write-Host "Updated: $backendCsproj" -ForegroundColor Yellow
    }
    
    # Update frontend package.json
    if (Test-Path $frontendPackageJson) {
        $packageJsonContent = Get-Content $frontendPackageJson -Raw | ConvertFrom-Json
        $packageJsonContent.version = $newVersion
        $packageJsonContent | ConvertTo-Json -Depth 10 | Out-File -FilePath $frontendPackageJson -Encoding UTF8 -NoNewline
        Write-Host "Updated: $frontendPackageJson" -ForegroundColor Yellow
    }
    
    # Update docker-compose.coolify.yml
    if (Test-Path $dockerCompose) {
        $dockerContent = Get-Content $dockerCompose -Raw
        $dockerContent = $dockerContent -replace 'backend:[\d\.]+}', "backend:$newVersion}"
        $dockerContent = $dockerContent -replace 'frontend:[\d\.]+}', "frontend:$newVersion}"
        $dockerContent | Out-File -FilePath $dockerCompose -Encoding UTF8 -NoNewline
        Write-Host "Updated: $dockerCompose" -ForegroundColor Yellow
    }
    
    Write-Host "`nVersion update complete!" -ForegroundColor Green
    Write-Host "Current version: $newVersion" -ForegroundColor Cyan
    
    # Show git status
    Write-Host "`nGit status:" -ForegroundColor Blue
    git status --short
    
    Write-Host "`nNext steps:" -ForegroundColor Magenta
    Write-Host "1. Review changes: git diff" -ForegroundColor Gray
    Write-Host "2. Commit changes: git add . && git commit -m `"Bump version to $newVersion`"" -ForegroundColor Gray
    Write-Host "3. Push to trigger Docker build: git push" -ForegroundColor Gray
    Write-Host "4. Create release tag: git tag v$newVersion && git push --tags" -ForegroundColor Gray
}

function Increment-Version {
    param(
        [string]$currentVersion,
        [string]$incrementType
    )
    
    $versionParts = $currentVersion.Split('.')
    if ($versionParts.Length -ne 3) {
        throw "Invalid version format. Expected format: x.y.z"
    }
    
    $major = [int]$versionParts[0]
    $minor = [int]$versionParts[1]
    $patch = [int]$versionParts[2]
    
    switch ($incrementType.ToLower()) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
        default {
            throw "Invalid increment type. Use: major, minor, or patch"
        }
    }
    
    return "$major.$minor.$patch"
}

# Main execution
try {
    $currentVersion = Get-CurrentVersion
    Write-Host "Current version: $currentVersion" -ForegroundColor Cyan
    
    if ($Action -eq "set") {
        if ([string]::IsNullOrEmpty($Version)) {
            Write-Host "Please provide a version number when using 'set'. Example: .\update-version.ps1 set 1.3.0" -ForegroundColor Red
            exit 1
        }
        
        # Validate version format
        if ($Version -notmatch '^\d+\.\d+\.\d+$') {
            Write-Host "Invalid version format. Please use semantic versioning (x.y.z)" -ForegroundColor Red
            exit 1
        }
        
        Set-Version -newVersion $Version
    }
    elseif ($Action -in @("major", "minor", "patch")) {
        $newVersion = Increment-Version -currentVersion $currentVersion -incrementType $Action
        Set-Version -newVersion $newVersion
    }
    else {
        Write-Host "Usage:" -ForegroundColor Yellow
        Write-Host "  .\update-version.ps1 [major|minor|patch]  - Increment version" -ForegroundColor Gray
        Write-Host "  .\update-version.ps1 set [version]        - Set specific version" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Examples:" -ForegroundColor Yellow
        Write-Host "  .\update-version.ps1 patch     # 1.0.0 -> 1.0.1" -ForegroundColor Gray
        Write-Host "  .\update-version.ps1 minor     # 1.0.1 -> 1.1.0" -ForegroundColor Gray
        Write-Host "  .\update-version.ps1 major     # 1.1.0 -> 2.0.0" -ForegroundColor Gray
        Write-Host "  .\update-version.ps1 set 1.5.0 # Set to 1.5.0" -ForegroundColor Gray
        exit 1
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}