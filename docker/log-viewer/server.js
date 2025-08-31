const express = require('express');
const fs = require('fs').promises;
const path = require('path');
const WebSocket = require('ws');
const http = require('http');

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const LOG_DIR = process.env.LOG_DIR || '/logs';
const PORT = process.env.PORT || 8888;

// Serve static files
app.use(express.static(path.join(__dirname, 'public')));

// Get list of log files
app.get('/api/logs', async (req, res) => {
    try {
        const files = await fs.readdir(LOG_DIR);
        const logFiles = files
            .filter(file => file.endsWith('.log'))
            .map(file => ({
                name: file,
                path: path.join(LOG_DIR, file)
            }))
            .sort((a, b) => b.name.localeCompare(a.name)); // Sort newest first
        
        res.json(logFiles);
    } catch (error) {
        console.error('Error reading log directory:', error);
        res.status(500).json({ error: 'Failed to read log files' });
    }
});

// Get log file content
app.get('/api/logs/:filename', async (req, res) => {
    try {
        const filename = req.params.filename;
        const filePath = path.join(LOG_DIR, filename);
        
        // Security check - ensure file is within log directory
        const resolvedPath = path.resolve(filePath);
        const resolvedLogDir = path.resolve(LOG_DIR);
        if (!resolvedPath.startsWith(resolvedLogDir)) {
            return res.status(403).json({ error: 'Access denied' });
        }

        const lines = req.query.lines ? parseInt(req.query.lines) : 100;
        const content = await fs.readFile(filePath, 'utf8');
        
        // Parse JSON log entries
        const logEntries = [];
        const jsonBlocks = content.split('\n\n').filter(block => block.trim());
        
        for (const block of jsonBlocks) {
            try {
                const entry = JSON.parse(block);
                logEntries.push(entry);
            } catch (e) {
                // Skip invalid JSON blocks
            }
        }
        
        // Get last N entries
        const recentEntries = logEntries.slice(-lines);
        
        res.json({
            filename,
            totalEntries: logEntries.length,
            entries: recentEntries
        });
    } catch (error) {
        console.error('Error reading log file:', error);
        res.status(500).json({ error: 'Failed to read log file' });
    }
});

// WebSocket for real-time log updates
wss.on('connection', (ws) => {
    console.log('Client connected for real-time logs');
    
    // Send initial greeting
    ws.send(JSON.stringify({ type: 'connected', message: 'Connected to log viewer' }));
    
    ws.on('close', () => {
        console.log('Client disconnected from real-time logs');
    });
});

// Watch for new log entries (simplified polling approach)
setInterval(async () => {
    try {
        const files = await fs.readdir(LOG_DIR);
        const todayFile = `${new Date().toISOString().split('T')[0]}.log`;
        
        if (files.includes(todayFile)) {
            const filePath = path.join(LOG_DIR, todayFile);
            const stats = await fs.stat(filePath);
            
            // Broadcast file size changes (simple new content detection)
            wss.clients.forEach(client => {
                if (client.readyState === WebSocket.OPEN) {
                    client.send(JSON.stringify({
                        type: 'file_update',
                        filename: todayFile,
                        size: stats.size,
                        modified: stats.mtime
                    }));
                }
            });
        }
    } catch (error) {
        console.error('Error watching logs:', error);
    }
}, 5000); // Check every 5 seconds

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({ status: 'healthy', logDir: LOG_DIR, time: new Date().toISOString() });
});

server.listen(PORT, () => {
    console.log(`Log viewer running on port ${PORT}`);
    console.log(`Log directory: ${LOG_DIR}`);
});