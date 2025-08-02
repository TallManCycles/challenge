# Testing Coolify Docker Compose Locally

This guide explains how to test your Coolify docker-compose configuration locally before deploying to Coolify.

## Quick Test Method

### Option 1: Use the Test Script (Recommended)
```bash
.\test-coolify-local.bat
```

This script will:
1. Check Docker is running
2. Copy test environment variables
3. Deploy the full stack with PostgreSQL
4. Test all endpoints
5. Display service status and URLs

### Option 2: Manual Testing
```bash
# 1. Copy test environment
copy docker\.env.coolify-test .env

# 2. Deploy with Coolify test compose
docker-compose -f docker/docker-compose.coolify-test.yml up --build -d

# 3. Check status
docker-compose -f docker/docker-compose.coolify-test.yml ps

# 4. View logs
docker-compose -f docker/docker-compose.coolify-test.yml logs -f

# 5. Stop when done
docker-compose -f docker/docker-compose.coolify-test.yml down
```

## What Gets Tested

### Services Deployed:
- **PostgreSQL**: postgres:16 with persistent data
- **Backend API**: ASP.NET Core with health checks
- **Frontend**: Vue.js application

### Service URLs:
- Frontend: http://localhost (port 80)
- Backend: http://localhost:8080
- Health Check: http://localhost:8080/api/health
- Swagger: http://localhost:8080/swagger
- PostgreSQL: localhost:5432

### Configuration Tested:
- Container communication (postgres ↔ backend ↔ frontend)
- Environment variable substitution
- Health checks and dependencies
- Volume persistence
- Port mappings

## Test Files

### `docker/docker-compose.coolify-test.yml`
- Full Coolify compose with all services uncommented
- Uses same structure as production Coolify deployment
- Includes PostgreSQL, backend, and frontend

### `docker/.env.coolify-test`
- Test environment variables
- Uses container names for service communication
- Safe test credentials (don't use in production)

### `test-coolify-local.bat`
- Automated test script
- Tests all endpoints
- Shows service status

## Differences from Production Coolify

| Aspect | Local Test | Production Coolify |
|--------|------------|-------------------|
| PostgreSQL | Container service | Container or external service |
| Domain | localhost | Custom domain with SSL |
| Environment | Test values | Production secrets |
| Networking | Docker default | Coolify's Traefik proxy |

## Troubleshooting

### Common Issues:

1. **Port Conflicts**:
   ```bash
   # Check what's using ports
   netstat -an | findstr ":80 "
   netstat -an | findstr ":8080 "
   netstat -an | findstr ":5432 "
   ```

2. **Service Not Starting**:
   ```bash
   # Check specific service logs
   docker-compose -f docker/docker-compose.coolify-test.yml logs backend
   docker-compose -f docker/docker-compose.coolify-test.yml logs postgres
   ```

3. **Database Connection Issues**:
   ```bash
   # Test database connection
   docker-compose -f docker/docker-compose.coolify-test.yml exec backend curl http://localhost:8080/api/health
   ```

4. **Frontend Not Loading**:
   ```bash
   # Check frontend logs
   docker-compose -f docker/docker-compose.coolify-test.yml logs frontend
   ```

### Cleanup Commands:

```bash
# Stop all services
docker-compose -f docker/docker-compose.coolify-test.yml down

# Stop and remove volumes (deletes database data)
docker-compose -f docker/docker-compose.coolify-test.yml down -v

# Remove all containers and images
docker-compose -f docker/docker-compose.coolify-test.yml down --rmi all -v

# Clean up test environment file
del .env
```

## Validation Checklist

Before deploying to Coolify, ensure:

- [ ] All services start successfully
- [ ] Health checks pass
- [ ] Backend API responds at `/api/health`
- [ ] Frontend loads at http://localhost
- [ ] Database connection works
- [ ] Environment variables are properly substituted
- [ ] No port conflicts or errors in logs

## Next Steps

Once local testing passes:

1. **Update Coolify Environment Variables**: Use production values
2. **Deploy to Coolify**: Use `docker/docker-compose.coolify.yml`
3. **Configure Domain**: Set up custom domain in Coolify
4. **Monitor**: Check Coolify logs and metrics