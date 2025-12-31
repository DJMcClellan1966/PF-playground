# 🚀 PocketFence AI Kernel

## CLI Mode (Current)
Run the application normally for interactive command-line interface:

```bash
# Standard CLI mode
dotnet run

# Available commands in CLI:
pocketfence> help              # Show all commands
pocketfence> check google.com  # Check URL safety
pocketfence> analyze "hello"   # Analyze text content
pocketfence> stats             # Show statistics
pocketfence> kernel            # Show kernel information
pocketfence> api               # Show API information
```

## Kernel API Mode
Start the kernel with REST API for application integration:

```bash
# Start kernel API mode
dotnet run -- --kernel

# Start as system service
dotnet run -- --service
```

### API Endpoints

**Content Filtering:**
```bash
# Check URL safety
curl -X POST http://localhost:5000/api/filter/url \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.com"}'

# Analyze text content  
curl -X POST http://localhost:5000/api/filter/content \
  -H "Content-Type: application/json" \
  -d '{"content":"Hello world"}'

# Batch analysis
curl -X POST http://localhost:5000/api/filter/batch \
  -H "Content-Type: application/json" \
  -d '{"items":[
    {"id":"1","type":"url","content":"google.com"},
    {"id":"2","type":"content","content":"test message"}
  ]}'
```

**Kernel Management:**
```bash
# Health check
curl http://localhost:5000/api/kernel/health

# List plugins
curl http://localhost:5000/api/kernel/plugins

# Statistics
curl http://localhost:5000/api/filter/stats
```

### API Documentation
When running in kernel mode, visit:
- **Swagger UI**: http://localhost:5000/swagger
- **API Root**: http://localhost:5000/

## Plugin System

Create custom plugins by implementing `IKernelPlugin`:

```csharp
public class MyCustomPlugin : IKernelPlugin
{
    public string Name => "MyCustomPlugin";
    public string Version => "1.0.0";
    public string Description => "Custom filtering logic";

    public async Task InitializeAsync()
    {
        // Initialize your plugin
    }

    public async Task<PluginResponse> ProcessAsync(PluginRequest request)
    {
        // Your custom filtering logic
        return new PluginResponse
        {
            IsBlocked = false,
            ThreatScore = 0.1,
            Reason = "Custom analysis result"
        };
    }

    public async Task ShutdownAsync()
    {
        // Cleanup
    }
}
```

Place plugin DLLs in the `plugins/` directory.

## Application Integration

Applications can connect to the kernel for centralized filtering:

### Example: Browser Extension
```javascript
// Check if URL is safe
const response = await fetch('http://localhost:5000/api/filter/url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url: currentUrl })
});

const result = await response.json();
if (result.isBlocked) {
    // Block the page
}
```

### Example: Chat Application
```csharp
// Analyze message content
var client = new HttpClient();
var request = new { content = userMessage };
var response = await client.PostAsJsonAsync(
    "http://localhost:5000/api/filter/content", request);
    
var result = await response.Content.ReadFromJsonAsync<ContentAnalysisResult>();
if (!result.IsChildSafe) {
    // Filter or flag the message
}
```

## Service Deployment

### Windows Service
```bash
# Build and install as Windows service
dotnet publish -c Release
sc create PocketFenceKernel binpath="path\to\PocketFence-AI.exe --service"
sc start PocketFenceKernel
```

### Linux Systemd
```bash
# Create systemd service file
sudo nano /etc/systemd/system/pocketfence.service

# Add service configuration
[Unit]
Description=PocketFence Kernel
After=network.target

[Service]
Type=exec
User=pocketfence
ExecStart=/path/to/PocketFence-AI --service
Restart=always

[Install]
WantedBy=multi-user.target

# Enable and start
sudo systemctl enable pocketfence
sudo systemctl start pocketfence
```

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 5000
ENTRYPOINT ["dotnet", "PocketFence-AI.dll", "--kernel"]
```

```bash
# Build and run container
docker build -t pocketfence-kernel .
docker run -d -p 5000:5000 pocketfence-kernel
```

## Architecture Benefits

### Centralized Filtering
- Single kernel serves multiple applications
- Consistent filtering rules across ecosystem
- Shared threat intelligence and learning

### Performance Optimizations  
- Cached results avoid redundant analysis
- Batch processing for high-volume requests
- Memory-efficient algorithmic approach

### Extensibility
- Plugin system for custom filtering logic
- REST API for any programming language
- Event-driven architecture for real-time updates

### Enterprise Features
- Service/daemon mode for production deployment
- Health monitoring and statistics
- Cross-platform compatibility
- Horizontal scaling capabilities

## Next Steps
1. **Build Enhanced Kernel**: Current implementation ✅
2. **Create Sample Applications**: Browser extension, chat app, etc.
3. **Plugin Marketplace**: Develop specialized filtering plugins
4. **Cloud Deployment**: Scale kernel as hosted service
5. **Family Dashboard**: Centralized management interface

The kernel is now ready to power a complete content filtering ecosystem! 🛡️