# Coolify Deployment Guide

This guide explains how to deploy your ASP.NET Core backend on Coolify with external PostgreSQL.

## Prerequisites

1. **Coolify Account**: Set up your Coolify instance
2. **PostgreSQL Database**: External PostgreSQL database (can be on the same server or separate)
3. **Git Repository**: Your code pushed to a Git repository (GitHub, GitLab, etc.)

## Deployment Methods

### Method 1: Docker Application (Recommended for Backend Only)

1. **Create New Application**
   - Go to Coolify dashboard
   - Click "Create Resource" → "Application"
   - Select "Public Repository" and enter your repository URL

2. **Configure Build Settings**
   - **Build Pack**: Docker
   - **Build Context**: `./backend`
   - **Dockerfile Location**: `backend/Dockerfile`
   - **Port**: `8080`

3. **Set Environment Variables**
   Go to "Environment Variables" section and add:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   POSTGRES_HOST=your-postgres-host
   POSTGRES_PORT=5432
   POSTGRES_DB=garmin_challenge
   POSTGRES_USER=your_username
   POSTGRES_PASSWORD=your_password
   JWT_KEY=your-64-character-secure-jwt-key
   AUTH_SALT=your-secure-salt
   FRONTEND_ORIGIN_0=https://your-frontend-domain.com
   FRONTEND_ORIGIN_1=https://www.your-frontend-domain.com
   ```

4. **Deploy**
   - Click "Deploy" button
   - Coolify will build and start your application

### Method 2: Docker Compose (For Full Stack)

1. **Create New Resource**
   - Choose "Docker Compose" instead of "Application"
   - Connect your repository

2. **Configure Compose File**
   - Use `docker/docker-compose.coolify.yml` as your compose file
   - Set the same environment variables as Method 1

## PostgreSQL Setup Options

### Option A: Coolify PostgreSQL Service
1. Create a new "Database" resource in Coolify
2. Choose PostgreSQL
3. Note the connection details
4. Use the internal hostname in your environment variables

### Option B: External PostgreSQL
1. Set up PostgreSQL on a separate server/service
2. Ensure it's accessible from your Coolify server
3. Configure firewall rules if needed
4. Use the external hostname/IP in environment variables

## Security Considerations

1. **Generate Secure Keys**:
   ```bash
   # JWT Key (64 characters)
   openssl rand -base64 48
   
   # Auth Salt
   openssl rand -base64 32
   ```

2. **Database Security**:
   - Use strong passwords
   - Limit database access to your application only
   - Consider using SSL connections

3. **Environment Variables**:
   - Mark sensitive variables as "Secret" in Coolify
   - Never commit secrets to your repository

## Custom Domain Setup

1. **Add Domain in Coolify**:
   - Go to "Domains" section
   - Add your custom domain
   - Coolify will automatically provision SSL certificate

2. **Update DNS**:
   - Point your domain to your Coolify server IP
   - Wait for DNS propagation

## Monitoring & Logs

- **View Logs**: Coolify dashboard → Application → Logs
- **Health Check**: Your app will be available at `/api/health`
- **Swagger Documentation**: Available at `/swagger`

## Troubleshooting

### Common Issues:

1. **Database Connection Failed**:
   - Verify PostgreSQL is running and accessible
   - Check connection string format
   - Verify credentials

2. **Build Failures**:
   - Check Dockerfile exists in `backend/` directory
   - Verify all required files are in repository
   - Check build logs for specific errors

3. **Environment Variables**:
   - Ensure all required variables are set
   - Check for typos in variable names
   - Verify values don't contain special characters that need escaping

### Useful Commands:

```bash
# Test database connection from Coolify server
psql -h your-postgres-host -U your-username -d garmin_challenge

# Check if port 8080 is accessible
curl http://your-coolify-domain:8080/api/health
```

## CI/CD Integration

To set up automated deployments:

1. **Disable Auto Deploy** in Coolify (Advanced Settings)
2. **Get Webhook URL** from Coolify (Webhooks section)
3. **Add to GitHub Actions**:
   ```yaml
   - name: Deploy to Coolify
     run: |
       curl -X POST "https://your-coolify-instance.com/api/v1/deploy?uuid=YOUR_UUID&force=false" \
         -H "Authorization: Bearer ${{ secrets.COOLIFY_TOKEN }}"
   ```

## Environment Files Reference

- `.env.coolify` - Template for Coolify environment variables
- `docker/docker-compose.coolify.yml` - Optimized for Coolify deployment