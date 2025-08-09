#!/bin/bash

# Create and push a version tag
# Usage: ./create-version.sh [version]
# Example: ./create-version.sh 1.1.0
# Example: ./create-version.sh 1.2.0

set -e

VERSION="$1"

if [ -z "$VERSION" ]; then
    echo "❌ Version is required"
    echo "Usage: $0 <version>"
    echo "Example: $0 1.1.0"
    exit 1
fi

# Validate version format (basic semver check)
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+(\.[0-9]+)?$ ]]; then
    echo "❌ Invalid version format. Use semantic versioning like 1.0.0 or 1.1"
    exit 1
fi

# Check if tag already exists
if git tag --list | grep -q "^v$VERSION$"; then
    echo "❌ Tag v$VERSION already exists"
    exit 1
fi

echo "🏷️  Creating version tag: v$VERSION"

# Create annotated tag
git tag -a "v$VERSION" -m "Release version $VERSION"

echo "✅ Tag v$VERSION created locally"
echo "🚀 Pushing tag to trigger Docker image build..."

# Push the tag
git push origin "v$VERSION"

echo "✅ Tag v$VERSION pushed to GitHub"
echo "📦 GitHub Actions will now build and push Docker images for version $VERSION"
echo "🔗 Check the build status at: https://github.com/$(git config --get remote.origin.url | sed 's/.*://; s/.git$//')/actions"

echo
echo "📋 Once the build completes, you can deploy with:"
echo "  ./deploy-version.sh $VERSION"
echo "  deploy-version.bat $VERSION"