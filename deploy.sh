#!/bin/bash
# Deploy script - builds and deploys new version

set -e

VERSION_TYPE=${1:-"patch"}

echo "🚀 Starting deployment process..."

# Read current version
CURRENT_VERSION=$(cat VERSION 2>/dev/null || echo "1.1.0")
echo "📋 Current version: $CURRENT_VERSION"

# Update version
echo "📈 Updating version ($VERSION_TYPE)..."
./update-version.sh $VERSION_TYPE

# Read new version
NEW_VERSION=$(cat VERSION)
echo "✨ New version: $NEW_VERSION"

# Commit and push version changes
echo "📤 Committing and pushing changes..."
git add VERSION backend/backend.csproj frontend/package.json docker/docker-compose.coolify.yml
git commit -m "Bump version to $NEW_VERSION" || echo "No changes to commit"
git push origin master

echo "🏗️  GitHub Actions will now build Docker images for version $NEW_VERSION"
echo "⏳ Wait for the build to complete, then deploy using the updated docker-compose.yml"
echo ""
echo "📝 Next steps:"
echo "1. Monitor GitHub Actions: https://github.com/TallManCycles/challenge/actions"
echo "2. Once build completes, deploy with: docker-compose -f docker/docker-compose.coolify.yml up -d"
echo "3. Or wait for Coolify auto-deployment if configured"