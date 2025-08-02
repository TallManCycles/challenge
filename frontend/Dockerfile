# Use Node.js 20 for building
FROM node:20-alpine AS build
WORKDIR /app

# Accept build arguments
ARG VITE_APP_API_ENDPOINT
ENV VITE_APP_API_ENDPOINT=$VITE_APP_API_ENDPOINT

# Copy package files
COPY package*.json ./
RUN npm ci --prefer-offline

# Copy source code and build
COPY . ./
RUN npm run build

# Use nginx for serving static files
FROM nginx:alpine AS runtime

# Install curl for health checks
RUN apk add --no-cache curl

# Copy built files to nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config for SPA
COPY <<EOF /etc/nginx/conf.d/default.conf
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # Handle SPA routing
    location / {
        try_files \$uri \$uri/ /index.html;
    }

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    
    # Caching for static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
EOF

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/ || exit 1

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]