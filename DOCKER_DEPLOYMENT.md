# Docker Deployment Guide

This guide explains how to deploy your application using versioned Docker images built and hosted on GitHub Container Registry.

## Overview

The application uses a CI/CD pipeline that automatically builds and versions Docker images on every push to the `master` branch. This eliminates the need to build Docker images locally during deployment.

### How It Works

1. **GitHub Actions** automatically builds frontend and backend Docker images
2. Images are tagged with version numbers and pushed to **GitHub Container Registry**
3. Deployment uses these pre-built images instead of building locally
4. Version management allows for easy rollbacks and consistent deployments

## Image Versioning

Images are automatically tagged with multiple formats:

- `latest` - Latest stable version from master branch
- `master-<commit>` - Specific commit from master (e.g., `master-abc1234`)
- `v*.*.*` - Tagged releases (e.g., `v1.0.0`, `v1.2.3`)
- `<branch>` - Feature branch builds
- `pr-<number>` - Pull request builds

## Quick Start

### 1. Authentication

First, authenticate with GitHub Container Registry:

```bash
echo $GITHUB_TOKEN | docker login ghcr.io -u your-username --password-stdin
```

> **Note**: Create a Personal Access Token with `read:packages` permission at https://github.com/settings/tokens

### 2. Configure Environment

Copy and configure the environment file:

```bash
cp docker/.env.example docker/.env
```

Edit `docker/.env` and update:
- Repository name in `BACKEND_IMAGE` and `FRONTEND_IMAGE`
- Database credentials
- JWT keys and secrets
- Other application settings

### 3. Deploy

#### Option A: Using Deployment Scripts (Recommended)

**Linux/macOS:**
```bash
# Deploy latest version
./scripts/deploy-versioned.sh latest

# Deploy specific commit version
./scripts/deploy-versioned.sh master-abc1234

# Deploy tagged release
./scripts/deploy-versioned.sh v1.0.0

# Deploy only backend
./scripts/deploy-versioned.sh --backend-only latest
```

**Windows:**
```cmd
:: Deploy latest version
scripts\deploy-versioned.bat latest

:: Deploy specific commit version
scripts\deploy-versioned.bat master-abc1234

:: Deploy tagged release
scripts\deploy-versioned.bat v1.0.0

:: Deploy only backend
scripts\deploy-versioned.bat --backend-only latest
```

#### Option B: Manual Docker Compose

```bash
cd docker

# Set image versions in .env file
echo "BACKEND_IMAGE=ghcr.io/your-username/your-repo/backend:latest" >> .env
echo "FRONTEND_IMAGE=ghcr.io/your-username/your-repo/frontend:latest" >> .env

# Deploy
docker-compose -f docker-compose.versioned.yml up -d
```

## Management Commands

### Check Status
```bash
# Using script
./scripts/deploy-versioned.sh --status

# Manual
cd docker && docker-compose -f docker-compose.versioned.yml ps
```

### View Logs
```bash
# Using script
./scripts/deploy-versioned.sh --logs

# Manual
cd docker && docker-compose -f docker-compose.versioned.yml logs -f
```

### Stop Services
```bash
# Using script
./scripts/deploy-versioned.sh --stop

# Manual
cd docker && docker-compose -f docker-compose.versioned.yml down
```

## Available Images

Your Docker images are available at:

- **Backend**: `ghcr.io/your-username/your-repo/backend`
- **Frontend**: `ghcr.io/your-username/your-repo/frontend`

To see all available tags, visit:
- https://github.com/your-username/your-repo/pkgs/container/backend
- https://github.com/your-username/your-repo/pkgs/container/frontend

## Deployment Strategies

### Production Deployment

For production, use specific version tags rather than `latest`:

```bash
# Deploy a specific tested version
./scripts/deploy-versioned.sh v1.2.0
```

### Staging/Testing

For staging, you can use latest or specific commit versions:

```bash
# Deploy latest from master
./scripts/deploy-versioned.sh latest

# Deploy specific commit for testing
./scripts/deploy-versioned.sh master-a1b2c3d
```

### Rollback

Easy rollback to previous versions:

```bash
# Rollback to previous version
./scripts/deploy-versioned.sh v1.1.0
```

## Environment Variables

### Required Variables

Update these in your `docker/.env` file:

```env
# Docker Images (automatically managed by deployment script)
BACKEND_IMAGE=ghcr.io/your-username/your-repo/backend:latest
FRONTEND_IMAGE=ghcr.io/your-username/your-repo/frontend:latest

# Database
POSTGRES_PASSWORD=your_secure_password
JWT_KEY=your_super_secure_jwt_key_here
AUTH_SALT=your_secure_salt_here

# CORS Origins
FRONTEND_ORIGIN_0=https://your-domain.com
FRONTEND_ORIGIN_1=https://www.your-domain.com
```

### Optional Variables

```env
# Garmin OAuth (if using Garmin integration)
GARMINOAUTH_CONSUMER_KEY=your_garmin_key
GARMINOAUTH_CONSUMER_SECRET=your_garmin_secret
GARMINOAUTH_CALLBACK_URL=https://your-domain.com/auth/garmin/callback

# AWS SES (if using email features)
AWS_ACCESS_KEY=your_aws_access_key
AWS_SECRET_KEY=your_aws_secret_key
EMAIL_FROM_ADDRESS=noreply@your-domain.com
```

## Monitoring

### Health Checks

The deployment includes health checks for all services:

```bash
# Check health status
docker ps

# View health check logs
docker logs garmin-challenge-backend
docker logs garmin-challenge-frontend
```

### Service URLs

After deployment, services are available at:

- **Frontend**: http://localhost (port configured in `FRONTEND_PORT`)
- **Backend API**: http://localhost:8080 (port configured in `BACKEND_PORT`)
- **Database**: localhost:5432 (port configured in `POSTGRES_PORT`)

## Troubleshooting

### Authentication Issues

```bash
# Re-authenticate with GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u your-username --password-stdin
```

### Image Pull Failures

```bash
# Check if image exists
docker manifest inspect ghcr.io/your-username/your-repo/backend:latest

# Pull manually to see detailed error
docker pull ghcr.io/your-username/your-repo/backend:latest
```

### Service Startup Issues

```bash
# Check service logs
docker-compose -f docker-compose.versioned.yml logs backend
docker-compose -f docker-compose.versioned.yml logs frontend

# Check environment variables
docker-compose -f docker-compose.versioned.yml config
```

### Database Connection Issues

```bash
# Check PostgreSQL logs
docker-compose -f docker-compose.versioned.yml logs postgres

# Test database connection
docker exec -it garmin-challenge-postgres psql -U postgres -d garmin_challenge
```

## Security Notes

1. **Never commit sensitive data** to `.env` files
2. **Use specific version tags** in production rather than `latest`
3. **Regularly update base images** by rebuilding and deploying new versions
4. **Monitor security advisories** for your dependencies
5. **Use strong passwords** and JWT keys in production

## Migration from Build-Based Deployment

If you're currently using the build-based docker-compose.coolify.yml:

1. **Backup your current environment**: `cp docker/.env docker/.env.backup`
2. **Update repository name** in the deployment scripts and environment files
3. **Authenticate with GitHub Container Registry**
4. **Test with a staging deployment** first
5. **Switch to versioned deployment** once validated

The GitHub Actions workflow will automatically update the `docker-compose.coolify.yml` file to use versioned images when you push to master, but you can also manually switch by following this guide.