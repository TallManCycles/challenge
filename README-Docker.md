# Local Docker Deployment

This guide helps you run the full-stack application (frontend + backend) locally using Docker with the Coolify configuration.

## Prerequisites

- Docker Desktop for Windows must be installed and running

## Quick Start

1. **Start Docker Desktop**

2. **Run the application using docker-compose.coolify.yml:**
   ```bash
   docker-compose -f docker/docker-compose.coolify.yml up --build -d
   ```

3. **Access your application:**
   - Frontend: http://localhost:80
   - Backend API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger

## Configuration

### Environment Variables

The deployment uses `.env.local` file for configuration. You can modify the following settings:

```env
# Port Configuration
BACKEND_PORT=8080          # Backend API port
FRONTEND_PORT=3000         # Frontend port

# Authentication
JWT_KEY=your-jwt-key       # JWT signing key (change for production)
AUTH_SALT=your-salt        # Password hashing salt

# API Configuration  
VITE_API_URL=http://localhost:8080  # Frontend API endpoint

# Database
CONNECTION_STRING=Data Source=/app/data/garmin_challenge.db

# CORS Origins (add your domains)
FRONTEND_ORIGIN_0=http://localhost:5173
FRONTEND_ORIGIN_1=http://localhost:3000
```

### Customizing Configuration

1. Edit `.env.local` file to change ports, keys, or other settings
2. Restart services: `docker-compose --env-file .env.local up --build -d`

## Manual Docker Commands

### Start Services
```bash
docker-compose -f docker/docker-compose.coolify.yml up --build -d
```

### View Logs
```bash
# All services
docker-compose -f docker/docker-compose.coolify.yml logs -f

# Specific service
docker-compose -f docker/docker-compose.coolify.yml logs -f backend
docker-compose -f docker/docker-compose.coolify.yml logs -f frontend
docker-compose -f docker/docker-compose.coolify.yml logs -f postgres
```

### Stop Services
```bash
docker-compose -f docker/docker-compose.coolify.yml down
```

### Rebuild and Restart
```bash
docker-compose -f docker/docker-compose.coolify.yml up --build -d
```

## Data Persistence

- SQLite database is stored in `./data` directory
- Data persists between container restarts
- To reset database: delete `./data` directory and restart services

## Troubleshooting

### Docker Not Running
- Ensure Docker Desktop is started
- Check system tray for Docker icon

### Port Conflicts
- Change `BACKEND_PORT` or `FRONTEND_PORT` in `.env.local`
- Restart services

### Build Failures
- Clear Docker cache: `docker system prune -a`
- Rebuild: `docker-compose --env-file .env.local up --build -d`

### Database Issues
- Check logs: `docker-compose --env-file .env.local logs backend`
- Reset database: delete `./data` directory

## Production Deployment

⚠️ **Important**: 
- Change `JWT_KEY` and `AUTH_SALT` for production
- Update CORS origins in `.env.local`
- Use proper SSL certificates
- Use external database for production