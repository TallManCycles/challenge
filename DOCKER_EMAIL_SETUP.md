# Docker Email Service Setup Guide

## Quick Start

The email notification system is now integrated into all Docker Compose configurations. Here's how to configure it:

### Development (Mock Email Service)
For development, the system automatically uses a mock email service that logs email content instead of sending actual emails.

1. Copy `docker/.env.example` to `docker/.env`
2. Leave email settings empty or as-is:
   ```bash
   AWS_REGION=us-east-1
   AWS_ACCESS_KEY=
   AWS_SECRET_KEY=
   EMAIL_FROM_ADDRESS=noreply@localhost.com
   ```
3. Deploy with any deployment script:
   ```bash
   deploy-scripts/deploy-fullstack.bat
   ```

### Production (AWS SES)
For production, configure AWS SES credentials:

1. Set up AWS SES (see `docker/EMAIL_CONFIGURATION.md` for detailed instructions)
2. Update your `.env` file with AWS credentials:
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   AWS_REGION=us-east-1
   AWS_ACCESS_KEY=AKIA...
   AWS_SECRET_KEY=abc123...
   EMAIL_FROM_ADDRESS=noreply@yourdomain.com
   ```

## Updated Docker Compose Files

All Docker Compose files now include email configuration environment variables:

- `docker-compose.yml` - Main compose file
- `docker-compose.backend.yml` - Backend-only deployment  
- `docker-compose.coolify.yml` - Coolify/production deployment
- `docker-compose.external-db.yml` - External database deployment

## Environment Variables Added

| Variable | Description | Default |
|----------|-------------|---------|
| `AWS_REGION` | AWS region for SES | us-east-1 |
| `AWS_ACCESS_KEY` | AWS access key | (empty) |
| `AWS_SECRET_KEY` | AWS secret key | (empty) |
| `EMAIL_FROM_ADDRESS` | Sender email address | noreply@yourdomain.com |

## Testing Email Notifications

### Development Testing
1. Start the application with `deploy-fullstack.bat`
2. Create a challenge and add participants
3. Upload an activity to the challenge
4. Check backend logs for mock email output:
   ```
   Mock Challenge Notification - To: user@example.com, User: JohnDoe, Activity: Morning Ride
   ```

### Production Testing
1. Verify your sender email in AWS SES
2. Set `ASPNETCORE_ENVIRONMENT=Production` in `.env`
3. Add valid AWS credentials
4. Test with verified email addresses (if in SES sandbox mode)

## Deployment Commands

All existing deployment commands work unchanged:

```bash
# Full stack with PostgreSQL
deploy-scripts/deploy-fullstack.bat

# Backend only
deploy-scripts/deploy-backend.bat

# Frontend only  
deploy-scripts/deploy-frontend.bat

# With external PostgreSQL
deploy-scripts/deploy-with-postgres.bat
```

## Troubleshooting

1. **No emails in development**: Expected behavior - check logs for mock output
2. **AWS credentials error**: Verify credentials in `.env` file
3. **Email not verified**: Verify sender email in AWS SES console
4. **Sandbox restrictions**: Verify recipient emails or request production access

For detailed troubleshooting, see `docker/EMAIL_CONFIGURATION.md`.

## File Changes Summary

### Updated Files:
- `docker/docker-compose.yml` - Added email environment variables
- `docker/docker-compose.backend.yml` - Added email environment variables  
- `docker/docker-compose.coolify.yml` - Added email environment variables
- `docker/docker-compose.external-db.yml` - Added email environment variables
- `docker/.env` - Added email configuration section
- `docker/.env.example` - Added email configuration examples
- `docker/.env.coolify` - Added production email settings
- `backend/appsettings.Development.json` - Added email defaults
- `backend/appsettings.Production.json` - Added email configuration

### New Files:
- `docker/EMAIL_CONFIGURATION.md` - Comprehensive email setup guide
- `DOCKER_EMAIL_SETUP.md` - This quick setup guide

The email service is now fully integrated and ready for both development and production use!