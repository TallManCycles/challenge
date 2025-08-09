#!/bin/bash

# Deploy with versioned Docker images
# Usage: ./deploy-versioned.sh [version]
# Example: ./deploy-versioned.sh master-abc1234
# Example: ./deploy-versioned.sh v1.0.0
# Example: ./deploy-versioned.sh latest

set -e

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
DOCKER_DIR="$PROJECT_DIR/docker"
COMPOSE_FILE="$DOCKER_DIR/docker-compose.versioned.yml"
ENV_FILE="$DOCKER_DIR/.env"

# Default values
DEFAULT_VERSION="latest"
REPO_NAME="your-username/your-repo"  # Update this to match your GitHub repository

# Functions
show_usage() {
    echo "Usage: $0 [OPTIONS] [VERSION]"
    echo ""
    echo "Deploy the application using versioned Docker images from GitHub Container Registry"
    echo ""
    echo "Arguments:"
    echo "  VERSION          Docker image version to deploy (default: $DEFAULT_VERSION)"
    echo ""
    echo "Options:"
    echo "  -h, --help       Show this help message"
    echo "  -r, --repo       Set repository name (default: $REPO_NAME)"
    echo "  --backend-only   Deploy only the backend service"
    echo "  --frontend-only  Deploy only the frontend service"
    echo "  --status         Show deployment status"
    echo "  --logs           Show service logs"
    echo "  --stop           Stop all services"
    echo ""
    echo "Examples:"
    echo "  $0 latest                    # Deploy latest version"
    echo "  $0 master-abc1234           # Deploy specific commit version"
    echo "  $0 v1.0.0                   # Deploy tagged release"
    echo "  $0 --backend-only latest    # Deploy only backend"
    echo "  $0 --status                 # Show current status"
}

check_prerequisites() {
    if ! command -v docker &> /dev/null; then
        echo "❌ Docker is not installed or not in PATH"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        echo "❌ Docker Compose is not installed or not available"
        exit 1
    fi

    if [ ! -f "$ENV_FILE" ]; then
        echo "❌ Environment file not found: $ENV_FILE"
        echo "Please copy $DOCKER_DIR/.env.example to $ENV_FILE and configure it"
        exit 1
    fi

    echo "✅ Prerequisites check passed"
}

update_env_file() {
    local version=$1
    local backend_image="ghcr.io/$REPO_NAME/backend:$version"
    local frontend_image="ghcr.io/$REPO_NAME/frontend:$version"

    echo "📝 Updating environment file with version: $version"
    
    # Create a temporary file with updated image versions
    cp "$ENV_FILE" "$ENV_FILE.backup"
    
    # Update or add image environment variables
    # Use a temporary file for cross-platform compatibility (macOS/BSD requires backup extension for sed -i)
    TEMP_FILE=$(mktemp)
    
    if grep -q "^BACKEND_IMAGE=" "$ENV_FILE"; then
        sed "s|^BACKEND_IMAGE=.*|BACKEND_IMAGE=$backend_image|" "$ENV_FILE" > "$TEMP_FILE"
        mv "$TEMP_FILE" "$ENV_FILE"
    else
        echo "BACKEND_IMAGE=$backend_image" >> "$ENV_FILE"
    fi
    
    if grep -q "^FRONTEND_IMAGE=" "$ENV_FILE"; then
        TEMP_FILE=$(mktemp)
        sed "s|^FRONTEND_IMAGE=.*|FRONTEND_IMAGE=$frontend_image|" "$ENV_FILE" > "$TEMP_FILE"
        mv "$TEMP_FILE" "$ENV_FILE"
    else
        echo "FRONTEND_IMAGE=$frontend_image" >> "$ENV_FILE"
    fi

    echo "📝 Backend image: $backend_image"
    echo "📝 Frontend image: $frontend_image"
}

pull_images() {
    local version=$1
    echo "🔄 Pulling Docker images for version: $version"
    
    docker pull "ghcr.io/$REPO_NAME/backend:$version" || {
        echo "❌ Failed to pull backend image. Make sure the image exists and you're authenticated."
        echo "Run: echo \$GITHUB_TOKEN | docker login ghcr.io -u your-username --password-stdin"
        exit 1
    }
    
    docker pull "ghcr.io/$REPO_NAME/frontend:$version" || {
        echo "❌ Failed to pull frontend image. Make sure the image exists and you're authenticated."
        exit 1
    }
    
    echo "✅ Images pulled successfully"
}

deploy() {
    local version=$1
    local services=$2
    
    echo "🚀 Deploying version: $version"
    
    # Use docker-compose or docker compose based on what's available
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    cd "$DOCKER_DIR"
    
    if [ -n "$services" ]; then
        echo "📦 Deploying services: $services"
        $COMPOSE_CMD -f docker-compose.versioned.yml up -d $services
    else
        echo "📦 Deploying all services"
        $COMPOSE_CMD -f docker-compose.versioned.yml up -d
    fi
    
    echo "✅ Deployment completed"
    echo ""
    echo "📋 Service status:"
    $COMPOSE_CMD -f docker-compose.versioned.yml ps
}

show_status() {
    cd "$DOCKER_DIR"
    
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    echo "📋 Current deployment status:"
    $COMPOSE_CMD -f docker-compose.versioned.yml ps
}

show_logs() {
    cd "$DOCKER_DIR"
    
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    echo "📝 Service logs (press Ctrl+C to exit):"
    $COMPOSE_CMD -f docker-compose.versioned.yml logs -f
}

stop_services() {
    cd "$DOCKER_DIR"
    
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    echo "🛑 Stopping all services..."
    $COMPOSE_CMD -f docker-compose.versioned.yml down
    echo "✅ All services stopped"
}

# Main script
main() {
    local version="$DEFAULT_VERSION"
    local services=""
    local action="deploy"
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_usage
                exit 0
                ;;
            -r|--repo)
                REPO_NAME="$2"
                shift 2
                ;;
            --backend-only)
                services="backend postgres"
                shift
                ;;
            --frontend-only)
                services="frontend"
                shift
                ;;
            --status)
                action="status"
                shift
                ;;
            --logs)
                action="logs"
                shift
                ;;
            --stop)
                action="stop"
                shift
                ;;
            -*)
                echo "❌ Unknown option: $1"
                show_usage
                exit 1
                ;;
            *)
                version="$1"
                shift
                ;;
        esac
    done
    
    echo "🚀 Versioned Deployment Script"
    echo "Repository: $REPO_NAME"
    
    case $action in
        status)
            show_status
            ;;
        logs)
            show_logs
            ;;
        stop)
            stop_services
            ;;
        deploy)
            check_prerequisites
            update_env_file "$version"
            pull_images "$version"
            deploy "$version" "$services"
            ;;
    esac
}

# Run main function
main "$@"