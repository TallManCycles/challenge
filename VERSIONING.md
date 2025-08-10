# Version Management

This project uses a centralized versioning system that automatically syncs versions across all components.

## Version Files

- **VERSION**: Central version file (e.g., `1.2.0`)
- **backend/backend.csproj**: Contains version properties for the .NET backend
- **frontend/package.json**: Contains version for the Vue.js frontend
- **docker/docker-compose.coolify.yml**: Contains default Docker image versions

## Automated Version Management

### Scripts

Two version management scripts are available:

#### Windows (PowerShell)
```powershell
# Increment patch version (1.2.0 -> 1.2.1)
.\update-version.ps1 patch

# Increment minor version (1.2.0 -> 1.3.0) 
.\update-version.ps1 minor

# Increment major version (1.2.0 -> 2.0.0)
.\update-version.ps1 major

# Set specific version
.\update-version.ps1 set 1.5.0
```

#### Linux/Mac (Bash)
```bash
# Increment patch version (1.2.0 -> 1.2.1)
./update-version.sh patch

# Increment minor version (1.2.0 -> 1.3.0) 
./update-version.sh minor

# Increment major version (1.2.0 -> 2.0.0)
./update-version.sh major

# Set specific version
./update-version.sh set 1.5.0
```

### What the Scripts Update

When you run a version script, it automatically updates:

1. **VERSION** - Central version file
2. **backend/backend.csproj** - Assembly version, file version, and product version
3. **frontend/package.json** - Package version
4. **docker/docker-compose.coolify.yml** - Default Docker image tags

## Docker Image Versioning

### GitHub Actions Integration

The GitHub Actions workflow (`.github/workflows/docker-build.yml`) automatically:

1. Reads the version from the `VERSION` file
2. Tags Docker images with the current version
3. Pushes images to GitHub Container Registry (GHCR)
4. Updates the docker-compose file with the new image versions
5. Creates a `DOCKER_IMAGES.md` file with current image information

### Docker Images

Images are built and tagged as:
- `ghcr.io/tallmancycles/challenge/backend:1.2.0`
- `ghcr.io/tallmancycles/challenge/frontend:1.2.0`

Additional tags are created for:
- Branch commits: `ghcr.io/tallmancycles/challenge/backend:master-abc1234`
- Git tags: `ghcr.io/tallmancycles/challenge/backend:v1.2.0`

## Deployment Workflow

### 1. Development Process
```bash
# Make your changes
git add .
git commit -m "Add new feature"

# Update version (choose appropriate increment)
./update-version.sh minor  # or patch/major

# Review the changes
git diff

# Commit version changes
git add .
git commit -m "Bump version to 1.3.0"

# Push to trigger Docker build
git push
```

### 2. GitHub Actions Process
When you push to master:
1. GitHub Actions reads `VERSION` file
2. Builds Docker images with version tags
3. Pushes images to GHCR
4. Updates docker-compose.coolify.yml with new versions
5. Commits the updated docker-compose file back to the repository

### 3. Deployment Process
The updated docker-compose.coolify.yml can be used directly for deployment:

```bash
# Deploy using the latest version
cd docker
docker-compose -f docker-compose.coolify.yml up -d
```

## Manual Version Management

If you prefer to update versions manually:

### Backend (.NET)
Update the `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `backend/backend.csproj`:
```xml
<Version>1.2.0</Version>
<AssemblyVersion>1.2.0</AssemblyVersion>
<FileVersion>1.2.0</FileVersion>
```

### Frontend (Vue.js)
Update the `version` field in `frontend/package.json`:
```json
{
  "version": "1.2.0"
}
```

### Docker Compose
Update the image tags in `docker/docker-compose.coolify.yml`:
```yaml
services:
  backend:
    image: ${BACKEND_IMAGE:-ghcr.io/tallmancycles/challenge/backend:1.2.0}
  frontend:
    image: ${FRONTEND_IMAGE:-ghcr.io/tallmancycles/challenge/frontend:1.2.0}
```

### Central Version File
Update the `VERSION` file:
```
1.2.0
```

## Semantic Versioning

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR** (x.0.0): Breaking changes, incompatible API changes
- **MINOR** (1.x.0): New features, backwards compatible
- **PATCH** (1.2.x): Bug fixes, backwards compatible

## Release Tags

For official releases, create Git tags:

```bash
# After updating version and pushing
git tag v1.2.0
git push --tags
```

This will trigger additional Docker image builds with the tag version.

## Environment-Specific Overrides

You can override image versions using environment variables:

```bash
# Use specific backend version
export BACKEND_IMAGE=ghcr.io/tallmancycles/challenge/backend:1.1.0

# Use specific frontend version  
export FRONTEND_IMAGE=ghcr.io/tallmancycles/challenge/frontend:1.2.0

# Deploy with overrides
docker-compose -f docker-compose.coolify.yml up -d
```

## Troubleshooting

### Script Permissions
If you get permission errors on Linux/Mac:
```bash
chmod +x update-version.sh
```

### Dependencies
The shell script works better with these tools installed:
- `jq` - For JSON manipulation (optional, falls back to sed)
- `git` - For showing status and managing repository

### Manual Sync
If versions get out of sync, you can reset everything to match the VERSION file:
```bash
# Set all versions to match VERSION file
./update-version.sh set $(cat VERSION)
```

This ensures all files use the same version number.