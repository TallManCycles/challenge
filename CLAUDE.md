# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

This is a full-stack application with a Vue.js frontend and ASP.NET Core backend:

- **Backend** (`backend/`): ASP.NET Core 8.0 Web API with Entity Framework Core and SQLite
  - Minimal API setup with Swagger/OpenAPI documentation
  - Includes a sample WeatherForecast endpoint
  - Uses SQLite database with Entity Framework Core migrations

- **Frontend** (`frontend/`): Vue 3 application with TypeScript, Vite, and Pinia
  - Vue 3 with Composition API setup
  - TypeScript for type safety
  - Pinia for state management
  - Vue Router for client-side routing (currently no routes defined)
  - Vite for build tooling and development server

## Development Commands

### Frontend (Vue.js)
Navigate to `frontend/` directory first:

```bash
cd frontend
npm install              # Install dependencies
npm run dev             # Start development server with hot reload
npm run build           # Build for production (includes type checking)
npm run preview         # Preview production build
npm run test:unit       # Run unit tests with Vitest
npm run type-check      # Type check with vue-tsc
npm run lint            # Lint and fix with ESLint
npm run format          # Format code with Prettier
```

### Backend (ASP.NET Core)
Navigate to `backend/` directory first:

```bash
cd backend
dotnet restore          # Restore NuGet packages
dotnet run             # Start development server
dotnet build           # Build the application
dotnet test            # Run tests (if any)
```

For Entity Framework migrations:
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Key Technologies

- **Frontend**: Vue 3, TypeScript, Vite, Pinia, Vue Router, Vitest, ESLint, Prettier
- **Backend**: ASP.NET Core 8.0, Entity Framework Core, SQLite, Swagger/OpenAPI
- **Node.js requirement**: ^20.19.0 || >=22.12.0

## Project Structure Notes

- The Vue app uses `@` alias for `src/` directory imports
- Backend includes Entity Framework setup but Data directory appears empty (likely needs initial migration)
- Frontend has minimal routing setup and a basic counter store example
- Both projects include development tooling for linting, formatting, and testing