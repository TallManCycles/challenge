# Email Service Configuration

The Challenge Hub application includes email notification functionality using AWS Simple Email Service (SES). This document explains how to configure email services for different deployment scenarios.

## Environment Variables

The following environment variables control the email service configuration:

### AWS SES Configuration
- `AWS_REGION` - AWS region for SES (default: us-east-1)
- `AWS_ACCESS_KEY` - AWS access key with SES permissions
- `AWS_SECRET_KEY` - AWS secret key
- `EMAIL_FROM_ADDRESS` - Email address for sending notifications (must be verified in SES)

### Development vs Production Behavior

The application automatically uses different email service implementations based on the environment:

- **Development (`ASPNETCORE_ENVIRONMENT=Development`)**: Uses `MockEmailService` which logs email content instead of sending actual emails
- **Production (`ASPNETCORE_ENVIRONMENT=Production`)**: Uses `AwsSesEmailService` which sends emails via AWS SES

## AWS SES Setup

### 1. Create AWS Account and SES Setup
1. Create an AWS account if you don't have one
2. Navigate to AWS SES in your preferred region
3. Verify your sending email address or domain
4. If in SES sandbox mode, verify recipient email addresses for testing

### 2. Create IAM User for SES
1. Go to AWS IAM console
2. Create a new user for programmatic access
3. Attach the `AmazonSESFullAccess` policy (or create a custom policy with minimum required permissions)
4. Save the Access Key ID and Secret Access Key

### 3. Minimum Required SES Permissions
For production use, create a custom IAM policy with minimal permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ses:SendEmail",
                "ses:SendRawEmail"
            ],
            "Resource": "*"
        }
    ]
}
```

## Docker Compose Configuration Examples

### Development Environment (.env)
```bash
# Email settings for development (uses mock service)
ASPNETCORE_ENVIRONMENT=Development
AWS_REGION=us-east-1
AWS_ACCESS_KEY=
AWS_SECRET_KEY=
EMAIL_FROM_ADDRESS=noreply@localhost.com
```

### Production Environment (.env)
```bash
# Email settings for production (uses AWS SES)
ASPNETCORE_ENVIRONMENT=Production
AWS_REGION=us-east-1
AWS_ACCESS_KEY=AKIA...
AWS_SECRET_KEY=abc123...
EMAIL_FROM_ADDRESS=noreply@yourdomain.com
```

### Coolify/Production Deployment
For Coolify or other production deployments, set these environment variables in your deployment platform:

```bash
AWS_REGION=us-east-1
AWS_ACCESS_KEY=your_aws_access_key
AWS_SECRET_KEY=your_aws_secret_key
EMAIL_FROM_ADDRESS=noreply@yourdomain.com
```

## Email Notification Features

### When Notifications Are Sent
- When a user uploads a new activity that counts toward a challenge
- Only sent to other participants in the same challenge
- Only sent to users who have email notifications enabled in their settings

### Email Content
- Professional HTML template with challenge and activity details
- Plain text fallback version
- Includes participant name, activity name, challenge title, and progress metrics
- Responsive design for mobile devices

### User Preferences
- Users can enable/disable email notifications in their Settings page
- Notifications are enabled by default for new users
- Individual users can opt out without affecting others

## Testing Email Configuration

### Development Testing
In development mode, check the application logs to see mock email output:
```
Mock Challenge Notification - To: user@example.com, User: JohnDoe, Activity: Morning Ride, Challenge: January Miles, Value: 25.5 km
```

### Production Testing
1. Ensure your sender email is verified in AWS SES
2. If in SES sandbox mode, verify recipient email addresses
3. Upload a test activity and check logs for successful email sending
4. Monitor AWS SES console for sending statistics and bounce rates

## Troubleshooting

### Common Issues

1. **"Email address not verified" error**
   - Verify your sender email address in AWS SES console
   - Wait for verification to complete before testing

2. **"Message rejected" error**
   - Check if you're in SES sandbox mode
   - Verify recipient email addresses if in sandbox mode
   - Request production access from AWS if needed

3. **AWS credentials error**
   - Verify AWS_ACCESS_KEY and AWS_SECRET_KEY are correctly set
   - Ensure IAM user has SES permissions
   - Check AWS region is correct

4. **No emails being sent in development**
   - This is expected behavior - check application logs for mock email output
   - Set ASPNETCORE_ENVIRONMENT=Production to test actual email sending

### Logs to Check
- Application logs will show email service initialization
- Mock email service logs all email content in development
- AWS SES service logs successful/failed email attempts
- Check AWS CloudWatch for detailed SES metrics

## Security Considerations

1. **Never commit AWS credentials to version control**
2. **Use IAM roles instead of access keys when possible** (e.g., on EC2 instances)
3. **Implement least-privilege access** for SES permissions
4. **Monitor SES usage and costs** through AWS billing
5. **Set up bounce and complaint handling** for production use
6. **Consider using AWS Secrets Manager** for credential management in production

## Cost Considerations

- AWS SES pricing: $0.10 per 1,000 emails sent
- First 62,000 emails per month are free when sent from EC2
- Monitor usage through AWS billing dashboard
- Consider implementing rate limiting for high-volume applications