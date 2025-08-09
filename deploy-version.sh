#!/bin/bash

# Quick deployment script for versioned Docker images
# Usage: ./deploy-version.sh [version]
# Example: ./deploy-version.sh 1.0
# Example: ./deploy-version.sh master-abc1234
# Example: ./deploy-version.sh 1.2.0

set -e

VERSION=${1:-1.0}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$SCRIPT_DIR/docker"
ENV_FILE="$DOCKER_DIR/.env"

echo "🚀 Deploying version: $VERSION"

# Check if .env file exists
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Environment file not found: $ENV_FILE"
    echo "Please copy $DOCKER_DIR/.env.example to $ENV_FILE and configure it"
    exit 1
fi

# Update .env file with new image versions
echo "📝 Updating image versions..."

# Create a temporary file
TEMP_FILE=$(mktemp)

# Update the environment file
while IFS= read -r line; do
    if [[ $line =~ ^BACKEND_IMAGE= ]]; then
        echo "BACKEND_IMAGE=ghcr.io/tallmancycles/challenge/backend:$VERSION"
    elif [[ $line =~ ^FRONTEND_IMAGE= ]]; then
        echo "FRONTEND_IMAGE=ghcr.io/tallmancycles/challenge/frontend:$VERSION"
    else
        echo "$line"
    fi
done < "$ENV_FILE" > "$TEMP_FILE"

# Move temporary file back to .env
mv "$TEMP_FILE" "$ENV_FILE"

echo "✅ Environment file updated"

# Pull the images
echo "🔄 Pulling Docker images..."
docker pull "ghcr.io/tallmancycles/challenge/backend:$VERSION"
docker pull "ghcr.io/tallmancycles/challenge/frontend:$VERSION"

# Deploy using docker-compose
echo "📦 Deploying services..."
cd "$DOCKER_DIR"

# Use docker-compose or docker compose based on what's available
if command -v docker-compose &> /dev/null; then
    COMPOSE_CMD="docker-compose"
else
    COMPOSE_CMD="docker compose"
fi

$COMPOSE_CMD -f docker-compose.coolify.yml up -d

echo "✅ Deployment completed successfully!"
echo
echo "📋 Service status:"
$COMPOSE_CMD -f docker-compose.coolify.yml ps

echo
echo "🌐 Your application should be available at:"
echo "  Frontend: http://localhost (port from FRONTEND_PORT)"
echo "  Backend:  http://localhost:8080 (port from BACKEND_PORT)"
echo
echo "💡 To deploy a different version:"
echo "  ./deploy-version.sh 1.1.0         (deploy version 1.1.0)"
echo "  ./deploy-version.sh master-abc123  (deploy specific commit)"