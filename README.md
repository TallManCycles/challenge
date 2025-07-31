# Garmin Cycling Challenge App

A competitive cycling challenge platform where friends can create and participate in distance, elevation, or time-based cycling challenges using their Garmin activity data.

## 🚴‍♂️ Features

- **User Authentication**: Secure registration and login system
- **Garmin Integration**: Automatic activity sync from Garmin Connect
- **Challenge System**: Create and join public cycling challenges
- **Real-time Progress**: Live tracking of challenge progress and leaderboards
- **Multiple Challenge Types**: Distance, elevation gain, or time-based challenges

## 🏗️ Architecture

This is a full-stack application with separate frontend and backend projects:

- **Frontend**: Vue.js 3 with TypeScript, Pinia state management, and Tailwind CSS
- **Backend**: ASP.NET Core 8.0 Web API with Entity Framework Core and SQLite
- **Authentication**: JWT-based authentication with secure token management
- **Database**: SQLite with Entity Framework Core migrations
- **Testing**: Comprehensive test suites for both frontend (Vitest) and backend

## 🛠️ Tech Stack

### Frontend
- Vue.js 3 with Composition API
- TypeScript for type safety
- Pinia for state management
- Vue Router for routing
- Tailwind CSS for styling
- Vite for build tooling
- Vitest for unit testing

### Backend
- ASP.NET Core 8.0 Web API
- Entity Framework Core with SQLite
- JWT Bearer authentication
- Swagger/OpenAPI documentation
- Comprehensive logging system

## 🚀 Getting Started

### Prerequisites
- Node.js (^20.19.0 || >=22.12.0)
- .NET 8.0 SDK
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd challenge
   ```

2. **Set up the backend**
   ```bash
   cd backend
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

3. **Set up the frontend** (in a new terminal)
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

4. **Access the application**
   - Frontend: http://localhost:5173
   - Backend API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger

## 📝 Development Commands

### Frontend Commands
```bash
cd frontend
npm run dev          # Start development server
npm run build        # Build for production
npm run preview      # Preview production build
npm run test:unit    # Run unit tests
npm run type-check   # TypeScript type checking
npm run lint         # Lint and fix code
npm run format       # Format code with Prettier
```

### Backend Commands
```bash
cd backend
dotnet run           # Start development server
dotnet build         # Build the application
dotnet test          # Run tests
dotnet ef migrations add <Name>  # Create new migration
dotnet ef database update       # Apply migrations
```

## 🗂️ Project Structure

```
challenge/
├── backend/                 # ASP.NET Core Web API
│   ├── Controllers/         # API controllers
│   ├── Models/             # Data models
│   ├── Data/               # Entity Framework context
│   ├── Services/           # Business logic services
│   ├── Migrations/         # EF Core migrations
│   └── ...
├── backend.Tests/          # Backend unit tests
├── frontend/               # Vue.js application
│   ├── src/
│   │   ├── views/          # Vue components/pages
│   │   ├── stores/         # Pinia stores
│   │   ├── services/       # API services
│   │   ├── types/          # TypeScript types
│   │   └── ...
│   ├── ux/                 # UI/UX design files
│   └── ...
└── README.md
```

## 🔐 Authentication

The application uses JWT-based authentication:
- Register new accounts or log in with existing credentials
- Secure API endpoints with JWT bearer tokens
- Automatic token refresh and secure storage

## 🏃‍♂️ Development Workflow

1. **Start both servers**: Run the backend API and frontend development server
2. **Code changes**: Both servers support hot reload for rapid development
3. **Testing**: Run unit tests for both frontend and backend
4. **Type checking**: Use TypeScript for compile-time error detection
5. **Linting**: Maintain code quality with ESLint and Prettier

## 🧪 Testing

- **Frontend**: Unit tests with Vitest and Vue Test Utils
- **Backend**: Comprehensive test suite including controller and integration tests
- **Database**: Separate test database for isolated testing

## 📚 API Documentation

When running the backend, visit `/swagger` to explore the interactive API documentation with all available endpoints, request/response schemas, and the ability to test endpoints directly.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.