# Garmin Cycling Challenge App - Product Requirements Document

## 1. Overview

### Product Vision
A simple, competitive cycling challenge platform where friends can create and participate in distance, elevation, or time-based cycling challenges using their Garmin activity data.

### Key Features
- Simple user authentication and registration
- Garmin Connect integration for automatic activity sync
- Public challenge creation and participation
- Real-time challenge progress tracking
- Clean, responsive Vue.js frontend with custom CSS

## 2. Technical Architecture

### Frontend Stack
- **Framework**: Vue.js 3 with Options API (`defineComponent`)
- **Language**: TypeScript
- **Authentication**: Better Auth
- **Styling**: Custom CSS (no framework dependencies)
- **Build Tool**: Vite (recommended for Vue 3 + TypeScript)

### Backend Stack
- **Framework**: .NET 8 Web API
- **Database**: SQLite with Entity Framework Core
- **Authentication**: Integration with Better Auth
- **External APIs**: Garmin Connect IQ API

### Deployment
- **Target Platform**: Coolify (Docker-based deployment)
- **Architecture**: Separate frontend and backend projects for deployment flexibility

## 3. Database Schema

### Users Table
```sql
Users
- Id (int, PK)
- Email (string, unique)
- Username (string, unique)
- CreatedAt (datetime)
- UpdatedAt (datetime)
- GarminUserId (string, nullable)
- GarminAccessToken (string, nullable, encrypted)
- GarminRefreshToken (string, nullable, encrypted)
- GarminConnectedAt (datetime, nullable)
```

### Challenges Table
```sql
Challenges
- Id (int, PK)
- Title (string)
- Description (string, nullable)
- CreatedById (int, FK -> Users.Id)
- ChallengeType (enum: Distance, Elevation, Time)
- StartDate (datetime)
- EndDate (datetime)
- IsActive (boolean)
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

### ChallengeParticipants Table
```sql
ChallengeParticipants
- Id (int, PK)
- ChallengeId (int, FK -> Challenges.Id)
- UserId (int, FK -> Users.Id)
- JoinedAt (datetime)
- CurrentTotal (decimal) // Current accumulated value for the challenge type
- LastActivityDate (datetime, nullable)
```

### Activities Table
```sql
Activities
- Id (int, PK)
- UserId (int, FK -> Users.Id)
- GarminActivityId (string, unique)
- ActivityName (string)
- Distance (decimal) // in kilometers
- ElevationGain (decimal) // in meters
- MovingTime (int) // in seconds
- ActivityDate (datetime)
- CreatedAt (datetime)
```

## 4. API Endpoints

### Authentication Endpoints
```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET /api/auth/me
```

### User Management
```
GET /api/users/profile
PUT /api/users/profile
POST /api/users/connect-garmin // Initiates Garmin OAuth
DELETE /api/users/disconnect-garmin
```

### Challenges
```
GET /api/challenges // List public challenges with pagination
POST /api/challenges // Create new challenge
GET /api/challenges/{id} // Get challenge details with leaderboard
POST /api/challenges/{id}/join // Join a challenge
DELETE /api/challenges/{id}/leave // Leave a challenge
```

### Activities
```
POST /api/webhooks/garmin // Garmin webhook endpoint
GET /api/activities // User's activity history
```

## 5. Frontend Pages & Components

### Page Structure
```
/login - Authentication page
/register - User registration
/dashboard - Main dashboard with active challenges
/challenges - Browse/create challenges
/challenges/{id} - Challenge detail view
/settings - User settings and Garmin connection
/profile - User profile and activity history
```

### Key Components
- `ChallengeCard` - Display challenge info and join button
- `Leaderboard` - Show participant rankings
- `ProgressBar` - Visual progress towards challenge goal
- `ActivityList` - Display user activities
- `ChallengeForm` - Create new challenge form

## 6. User Flows

### Initial Setup Flow
1. User registers/logs in with Better Auth
2. User navigates to Settings
3. User clicks "Connect Garmin"
4. OAuth flow redirects to Garmin
5. User authorizes app
6. System automatically sets up webhooks
7. User can now participate in challenges

### Challenge Participation Flow
1. User browses public challenges
2. User joins a challenge
3. User's cycling activities automatically count toward challenge
4. User can view progress on challenge page
5. Challenge ends, winner is determined

### Challenge Creation Flow
1. User clicks "Create Challenge"
2. User fills form: title, description, type, start/end dates
3. Challenge becomes public and joinable
4. Creator automatically joins their own challenge

## 7. Garmin Integration

### OAuth Setup
- Use Garmin Connect IQ API
- Store access/refresh tokens securely
- Implement token refresh mechanism

### Webhook Configuration
- Automatically register webhook URL when user connects Garmin
- Webhook endpoint: `POST /api/webhooks/garmin`
- Process only cycling activities (activity type filtering)
- Update challenge participation totals in real-time

### Data Synchronization
- Only sync essential activity data
- Filter for cycling activities only
- Update challenge totals immediately when new activity received
- Handle duplicate activities (use Garmin activity ID)

## 8. Implementation Phases

### Phase 1: Core Infrastructure (Week 1-2)
- Set up .NET 8 API project with EF Core
- Configure SQLite database and migrations
- Implement Better Auth integration
- Create basic Vue.js 3 TypeScript project
- Set up Docker configuration for Coolify deployment

### Phase 2: User Management (Week 3)
- Implement user registration/login
- Create user profile management
- Build settings page structure
- Basic routing and navigation

### Phase 3: Garmin Integration (Week 4-5)
- Implement Garmin OAuth flow
- Set up webhook endpoint and processing
- Create activity data models and storage
- Test with real Garmin data

### Phase 4: Challenge System (Week 6-7)
- Build challenge creation functionality
- Implement challenge joining/leaving
- Create leaderboard calculations
- Build challenge detail pages

### Phase 5: Frontend Polish (Week 8)
- Implement responsive design
- Add progress visualizations
- Create dashboard with active challenges
- Add error handling and loading states

### Phase 6: Testing & Deployment (Week 9)
- End-to-end testing
- Performance optimizations
- Coolify deployment setup
- Production monitoring

## 9. Future Enhancements (Post-MVP)

### Potential Features
- Multi-metric challenges (distance + elevation)
- Private/invite-only challenges
- Challenge templates for easy monthly challenge creation
- Progress charts and detailed analytics
- Push notifications for challenge updates
- Team-based challenges
- Achievement badges and streaks
- Activity photos and comments

### Technical Improvements
- Redis caching for leaderboards
- Real-time updates with SignalR
- Mobile app (React Native or native)
- Advanced activity filtering options
- Bulk data sync for historical activities

## 10. Security Considerations

### Data Protection
- Encrypt Garmin tokens at rest
- Use HTTPS everywhere
- Implement rate limiting on API endpoints
- Validate all webhook payloads
- CORS configuration for frontend

### Privacy
- Users control their own data
- Clear data retention policies
- Option to delete account and all data
- Activity data only visible in joined challenges

## 11. Deployment Architecture

### Coolify Setup
```dockerfile
# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "GarminChallengeApi.dll"]
```

```dockerfile
# Frontend Dockerfile
FROM node:18-alpine
COPY . /app
WORKDIR /app
RUN npm install && npm run build
FROM nginx:alpine
COPY --from=0 /app/dist /usr/share/nginx/html
```

### Environment Variables
- `DATABASE_CONNECTION_STRING`
- `GARMIN_CLIENT_ID`
- `GARMIN_CLIENT_SECRET`
- `BETTER_AUTH_SECRET`
- `WEBHOOK_BASE_URL`

## 12. Success Metrics

### MVP Success Criteria
- Users can successfully connect Garmin accounts
- Activities automatically sync and update challenge progress
- Challenge creation and participation works smoothly
- Leaderboards update correctly
- App deploys successfully on Coolify

### Performance Targets
- Page load times < 2 seconds
- API response times < 500ms
- 99.9% uptime
- Support for 100+ concurrent users