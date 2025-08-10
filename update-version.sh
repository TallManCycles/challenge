#!/bin/bash
# Version Management Script
# Usage: ./update-version.sh [major|minor|patch] or ./update-version.sh set [version]

set -e

ACTION=${1:-"patch"}
VERSION=${2:-""}

# File paths
VERSION_FILE="VERSION"
BACKEND_CSPROJ="backend/backend.csproj"
FRONTEND_PACKAGE_JSON="frontend/package.json"
DOCKER_COMPOSE="docker/docker-compose.coolify.yml"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

get_current_version() {
    if [[ -f $VERSION_FILE ]]; then
        cat $VERSION_FILE
    else
        echo "1.0.0"
    fi
}

set_version() {
    local new_version=$1
    
    echo -e "${GREEN}Setting version to: $new_version${NC}"
    
    # Update VERSION file
    echo -n "$new_version" > $VERSION_FILE
    
    # Update backend .csproj
    if [[ -f $BACKEND_CSPROJ ]]; then
        if command -v sed >/dev/null 2>&1; then
            # Use GNU sed (Linux) or BSD sed (Mac)
            if sed --version >/dev/null 2>&1; then
                # GNU sed
                sed -i "s|<Version>[0-9.]*</Version>|<Version>$new_version</Version>|g" $BACKEND_CSPROJ
                sed -i "s|<AssemblyVersion>[0-9.]*</AssemblyVersion>|<AssemblyVersion>$new_version</AssemblyVersion>|g" $BACKEND_CSPROJ
                sed -i "s|<FileVersion>[0-9.]*</FileVersion>|<FileVersion>$new_version</FileVersion>|g" $BACKEND_CSPROJ
            else
                # BSD sed (Mac)
                sed -i '' "s|<Version>[0-9.]*</Version>|<Version>$new_version</Version>|g" $BACKEND_CSPROJ
                sed -i '' "s|<AssemblyVersion>[0-9.]*</AssemblyVersion>|<AssemblyVersion>$new_version</AssemblyVersion>|g" $BACKEND_CSPROJ
                sed -i '' "s|<FileVersion>[0-9.]*</FileVersion>|<FileVersion>$new_version</FileVersion>|g" $BACKEND_CSPROJ
            fi
        fi
        echo -e "${YELLOW}Updated: $BACKEND_CSPROJ${NC}"
    fi
    
    # Update frontend package.json
    if [[ -f $FRONTEND_PACKAGE_JSON ]]; then
        if command -v jq >/dev/null 2>&1; then
            # Use jq if available
            jq ".version = \"$new_version\"" $FRONTEND_PACKAGE_JSON > tmp.json && mv tmp.json $FRONTEND_PACKAGE_JSON
        else
            # Fallback to sed
            if sed --version >/dev/null 2>&1; then
                # GNU sed
                sed -i "s|\"version\": \"[^\"]*\"|\"version\": \"$new_version\"|g" $FRONTEND_PACKAGE_JSON
            else
                # BSD sed (Mac)
                sed -i '' "s|\"version\": \"[^\"]*\"|\"version\": \"$new_version\"|g" $FRONTEND_PACKAGE_JSON
            fi
        fi
        echo -e "${YELLOW}Updated: $FRONTEND_PACKAGE_JSON${NC}"
    fi
    
    # Update docker-compose.coolify.yml
    if [[ -f $DOCKER_COMPOSE ]]; then
        if sed --version >/dev/null 2>&1; then
            # GNU sed
            sed -i "s|backend:[0-9.]*}|backend:$new_version}|g" $DOCKER_COMPOSE
            sed -i "s|frontend:[0-9.]*}|frontend:$new_version}|g" $DOCKER_COMPOSE
        else
            # BSD sed (Mac)
            sed -i '' "s|backend:[0-9.]*}|backend:$new_version}|g" $DOCKER_COMPOSE
            sed -i '' "s|frontend:[0-9.]*}|frontend:$new_version}|g" $DOCKER_COMPOSE
        fi
        echo -e "${YELLOW}Updated: $DOCKER_COMPOSE${NC}"
    fi
    
    echo -e "\n${GREEN}Version update complete!${NC}"
    echo -e "${CYAN}Current version: $new_version${NC}"
    
    # Show git status
    echo -e "\n${BLUE}Git status:${NC}"
    git status --short
    
    echo -e "\n${MAGENTA}Next steps:${NC}"
    echo -e "${GRAY}1. Review changes: git diff${NC}"
    echo -e "${GRAY}2. Commit changes: git add . && git commit -m \"Bump version to $new_version\"${NC}"
    echo -e "${GRAY}3. Push to trigger Docker build: git push${NC}"
    echo -e "${GRAY}4. Create release tag: git tag v$new_version && git push --tags${NC}"
}

increment_version() {
    local current_version=$1
    local increment_type=$2
    
    # Split version into parts
    IFS='.' read -ra VERSION_PARTS <<< "$current_version"
    
    if [[ ${#VERSION_PARTS[@]} -ne 3 ]]; then
        echo -e "${RED}Invalid version format. Expected format: x.y.z${NC}" >&2
        exit 1
    fi
    
    local major=${VERSION_PARTS[0]}
    local minor=${VERSION_PARTS[1]}
    local patch=${VERSION_PARTS[2]}
    
    case $increment_type in
        "major")
            ((major++))
            minor=0
            patch=0
            ;;
        "minor")
            ((minor++))
            patch=0
            ;;
        "patch")
            ((patch++))
            ;;
        *)
            echo -e "${RED}Invalid increment type. Use: major, minor, or patch${NC}" >&2
            exit 1
            ;;
    esac
    
    echo "$major.$minor.$patch"
}

# Main execution
current_version=$(get_current_version)
echo -e "${CYAN}Current version: $current_version${NC}"

if [[ $ACTION == "set" ]]; then
    if [[ -z $VERSION ]]; then
        echo -e "${RED}Please provide a version number when using 'set'. Example: ./update-version.sh set 1.3.0${NC}" >&2
        exit 1
    fi
    
    # Validate version format
    if [[ ! $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        echo -e "${RED}Invalid version format. Please use semantic versioning (x.y.z)${NC}" >&2
        exit 1
    fi
    
    set_version "$VERSION"
elif [[ $ACTION =~ ^(major|minor|patch)$ ]]; then
    new_version=$(increment_version "$current_version" "$ACTION")
    set_version "$new_version"
else
    echo -e "${YELLOW}Usage:${NC}"
    echo -e "${GRAY}  ./update-version.sh [major|minor|patch]  - Increment version${NC}"
    echo -e "${GRAY}  ./update-version.sh set [version]        - Set specific version${NC}"
    echo ""
    echo -e "${YELLOW}Examples:${NC}"
    echo -e "${GRAY}  ./update-version.sh patch     # 1.0.0 -> 1.0.1${NC}"
    echo -e "${GRAY}  ./update-version.sh minor     # 1.0.1 -> 1.1.0${NC}"
    echo -e "${GRAY}  ./update-version.sh major     # 1.1.0 -> 2.0.0${NC}"
    echo -e "${GRAY}  ./update-version.sh set 1.5.0 # Set to 1.5.0${NC}"
    exit 1
fi