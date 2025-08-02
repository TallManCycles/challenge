# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish
 
# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks (before switching to non-root user)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published app
COPY --from=build /app/publish ./

# Create data directory and set ownership
RUN mkdir -p /app/data && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/garmin_challenge.db"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_EnableDiagnostics=0

# Security and JWT defaults (override these in deployment)
ENV Auth__JwtKey="DefaultJwtKey123456789012345678901234567890"
ENV Auth__Salt="DefaultSalt123!"

# CORS defaults (override these in deployment)
ENV Frontend__AllowedOrigins__0="http://localhost:5173"
ENV Frontend__AllowedOrigins__1="http://localhost:3000"
ENV Frontend__AllowedOrigins__2="https://your-frontend-domain.com"

# Logging configuration
ENV Logging__LogLevel__Default="Information"
ENV Logging__LogLevel__Microsoft_AspNetCore="Warning"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "backend.dll"]