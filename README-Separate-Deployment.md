# Separate Service Deployment

This guide explains how to deploy the frontend and backend services separately using individual Docker Compose files.

## Quick Start

### Deploy Both Services
```bash
.\deploy-fullstack.bat
```

### Deploy Backend Only
```bash
.\deploy-backend.bat
```

### Deploy Frontend Only
```bash
.\deploy-frontend.bat
```

## Manual Commands

### Backend Service

```bash
# Start backend
docker-compose -f docker/docker-compose.backend.yml up --build -d

# View backend logs
docker-compose -f docker/docker-compose.backend.yml logs -f

# Stop backend
docker-compose -f docker/docker-compose.backend.yml down

# Check backend status
docker-compose -f docker/docker-compose.backend.yml ps
```

**Backend will be available at:**
- API: http://localhost:8080
- Health Check: http://localhost:8080/api/health
- Swagger UI: http://localhost:8080/swagger

### Frontend Service

```bash
# Start frontend
docker-compose -f docker/docker-compose.frontend.yml up --build -d

# View frontend logs
docker-compose -f docker/docker-compose.frontend.yml logs -f

# Stop frontend
docker-compose -f docker/docker-compose.frontend.yml down

# Check frontend status
docker-compose -f docker/docker-compose.frontend.yml ps
```

**Frontend will be available at:**
- Application: http://localhost:3000

## Network Communication

Both services use a shared Docker network called `challenge-network`:
- Services can communicate internally using service names
- Frontend can reach backend at `http://backend:8080` (internal)
- External access uses `localhost` with mapped ports

## Environment Configuration

All services use the same `.env` file for configuration. Key variables:

```env
# Ports
BACKEND_PORT=8080
FRONTEND_PORT=3000

# API Endpoints
VITE_APP_API_ENDPOINT=http://localhost:8080

# Authentication
JWT_KEY=your-secret-key
AUTH_SALT=your-salt

# Database
CONNECTION_STRING=Data Source=/app/data/garmin_challenge.db
DATA_VOLUME_PATH=./data
```

## Benefits of Separate Deployment

✅ **Independent scaling** - Scale frontend and backend separately  
✅ **Independent updates** - Update one service without affecting the other  
✅ **Resource optimization** - Only run the services you need  
✅ **Development flexibility** - Develop and test services in isolation  
✅ **Production readiness** - Mirrors microservices architecture  

## Deployment Scenarios

### Development
```bash
# Backend only (for API development)
.\deploy-backend.bat

# Frontend only (for UI development with external API)
.\deploy-frontend.bat
```

### Testing
```bash
# Full stack for integration testing
.\deploy-fullstack.bat
```

### Production
- Deploy backend and frontend to separate containers/hosts
- Use environment variables to configure API endpoints
- Scale services independently based on load

## Troubleshooting

### Network Issues
If services can't communicate:
```bash
# Recreate the shared network
docker network rm challenge-network
docker network create challenge-network
```

### Port Conflicts
Change ports in `.env` file:
```env
BACKEND_PORT=9000
FRONTEND_PORT=4000
```

### Service Dependencies
- Frontend depends on backend for API calls
- Start backend first, then frontend
- Use `.\deploy-fullstack.bat` for correct startup order

## File Structure

```
├── docker/
│   ├── docker-compose.yml              # Original combined file (still works)
│   ├── docker-compose.backend.yml      # Backend service only
│   ├── docker-compose.frontend.yml     # Frontend service only
│   ├── docker-compose.postgres.yml     # PostgreSQL service only
│   ├── docker-compose.external-db.yml  # External database deployment
│   ├── docker-compose.coolify.yml      # Coolify deployment
│   └── docker-compose.coolify-test.yml # Local Coolify testing
├── deploy-backend.bat              # Backend deployment script
├── deploy-frontend.bat             # Frontend deployment script
├── deploy-fullstack.bat            # Deploy both services
└── .env                           # Shared environment configuration
```