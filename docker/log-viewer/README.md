# Challenge API Log Viewer

A web-based log viewer for the Challenge API that provides real-time monitoring and browsing of application logs.

## Features

- **Real-time Log Viewing**: Browse logs by date with automatic refresh capabilities
- **Multiple Log Levels**: Color-coded display for INFO, WARNING, and ERROR levels
- **Search and Filter**: Easy navigation through log files
- **WebSocket Updates**: Real-time notifications when new log entries are available
- **Responsive Design**: Clean, modern interface that works on all devices
- **Read-only Access**: Safe viewing without the ability to modify logs

## How it Works

1. **Log Collection**: Reads JSON log files created by the Challenge API's `FileLoggingService`
2. **Web Interface**: Provides a browser-based interface accessible on port 8888
3. **Real-time Updates**: Uses WebSocket connections to notify when log files are updated
4. **Docker Integration**: Shares the `backend_logs` volume for seamless log access

## API Endpoints

- `GET /` - Main log viewer interface
- `GET /api/logs` - List available log files
- `GET /api/logs/:filename` - Get content of a specific log file
- `GET /health` - Health check endpoint
- WebSocket connection for real-time updates

## Usage

### Via Docker Compose

The log viewer is integrated into the main docker-compose.coolify.yml file:

```yaml
services:
  log-viewer:
    build:
      context: ./log-viewer
      dockerfile: Dockerfile
    ports:
      - "8888:8888"
    volumes:
      - backend_logs:/logs:ro  # Read-only access to backend logs
```

### Environment Variables

- `LOG_DIR`: Directory containing log files (default: `/logs`)
- `PORT`: Port to run the web server on (default: `8888`)
- `NODE_ENV`: Node environment (set to `production` in Docker)

### Access

Once running, access the log viewer at:
- `http://localhost:8888` (local development)
- `http://your-server:8888` (production)

## Log Format

The viewer expects JSON log entries in the format created by the Challenge API's FileLoggingService:

```json
{
  "Timestamp": "2024-01-01T12:00:00.000Z",
  "Level": "INFO",
  "Category": "General",
  "Message": "Log message here",
  "Exception": "Stack trace if error"
}
```

## Security Features

- **Read-only Volume Mount**: Logs are mounted as read-only to prevent accidental modification
- **Non-root User**: Container runs as a non-privileged user
- **Path Validation**: Prevents directory traversal attacks
- **No Log Modification**: Only provides viewing capabilities, no editing or deletion

## Development

To run locally for development:

```bash
npm install
LOG_DIR=/path/to/logs npm start
```

## Building

To build the Docker image:

```bash
docker build -t challenge-log-viewer .
```

## Health Check

The service includes a health check endpoint at `/health` that returns:

```json
{
  "status": "healthy",
  "logDir": "/logs",
  "time": "2024-01-01T12:00:00.000Z"
}
```