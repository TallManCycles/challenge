FROM node:18-alpine

# Set working directory
WORKDIR /app

# Create logs directory
RUN mkdir -p /logs

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install --only=production

# Copy application files
COPY . .

# Create non-root user for security
RUN addgroup -g 1001 -S nodejs && \
    adduser -S logviewer -u 1001 -G nodejs

# Set ownership
RUN chown -R logviewer:nodejs /app /logs

# Switch to non-root user
USER logviewer

# Expose port
EXPOSE 8888

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD node -e "const http = require('http'); \
    const options = { hostname: 'localhost', port: 8888, path: '/health', timeout: 2000 }; \
    const req = http.request(options, (res) => { \
        if (res.statusCode === 200) process.exit(0); else process.exit(1); \
    }); \
    req.on('error', () => process.exit(1)); \
    req.end();"

# Start application
CMD ["npm", "start"]