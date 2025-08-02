# Deployment Guide

## Environment Variables Required

### **Critical Security Variables (MUST SET)**
```bash
# REQUIRED: Unique JWT secret key (minimum 32 characters)
Auth__JwtKey=your-super-secret-jwt-key-minimum-32-characters-long

# REQUIRED: Your frontend domain(s) for CORS
Frontend__AllowedOrigins__0=https://your-frontend-domain.com
Frontend__AllowedOrigins__1=https://www.your-frontend-domain.com
```

### **Backend Database Persistence**
- **Volume Mount**: `/app/data` 
- **Database Path**: `/app/data/garmin_challenge.db`
- **Permissions**: Container runs as non-root user

### **Railway Deployment**

**Backend:**
1. Set environment variables above
2. Mount volume at `/app/data`
3. Dockerfile will be auto-detected

**Frontend:**
1. No environment variables needed
2. Dockerfile will be auto-detected
3. Nginx serves static files

### **Environment Variable Examples**

**For Railway/Production:**
```bash
Auth__JwtKey=mysupersecretjwtkey123456789012345678901234567890
Frontend__AllowedOrigins__0=https://myapp-frontend.railway.app
Frontend__AllowedOrigins__1=https://myapp.com
```

**For Local Docker:**
```bash
docker run -d \
  -p 8080:8080 \
  -v ./data:/app/data \
  -e Auth__JwtKey=your-secret-key \
  -e Frontend__AllowedOrigins__0=http://localhost:5173 \
  your-backend-image
```

## Health Checks
- **Backend**: `GET /api/health`
- **Frontend**: `GET /` (nginx default)

## File Structure
```
backend/
├── Dockerfile              # Production Docker build
├── docker/
│   ├── .env.example        # Environment variable template
│   ├── .env.production     # Production environment template
│   ├── .env.coolify        # Coolify deployment template
│   └── .env.coolify-test   # Local Coolify testing
└── .dockerignore           # Exclude unnecessary files

frontend/
├── Dockerfile              # Nginx + Vue.js build
└── .dockerignore           # Exclude node_modules, etc.
```