# Redis Setup for Gaming Cafe Management Suite

## Development Setup (Windows)

### Option 1: Docker (Recommended)
```bash
# Pull and run Redis in Docker
docker run -d --name redis-gamingcafe -p 6379:6379 redis:latest

# To stop Redis
docker stop redis-gamingcafe

# To start again
docker start redis-gamingcafe
```

### Option 2: WSL2
```bash
# Install Redis on WSL2
sudo apt update
sudo apt install redis-server

# Start Redis
sudo service redis-server start

# Test connection
redis-cli ping
```

### Option 3: Windows Native
1. Download Redis for Windows from: https://github.com/microsoftarchive/redis/releases
2. Extract and run `redis-server.exe`
3. Default port: 6379

## Configuration

The application is configured to connect to `localhost:6379` by default.

Connection string is in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## Health Check

The application includes Redis health monitoring at `/health` endpoint:
- **Healthy**: Redis is connected and responding to operations
- **Unhealthy**: Redis connection failed or operations not working
- **Degraded**: Redis available but with warnings

## Cache Features

The Redis cache service provides:
- ✅ Generic object caching with JSON serialization
- ✅ Pattern-based cache invalidation
- ✅ Health monitoring and connection testing
- ✅ Automatic expiration management
- ✅ Graceful fallback when Redis unavailable

## Usage Examples

```csharp
// Inject ICacheService in your controllers/services
public class MyController : ControllerBase
{
    private readonly ICacheService _cache;
    
    public MyController(ICacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<ActionResult> GetData(int id)
    {
        var cacheKey = $"data_{id}";
        
        // Try to get from cache first
        var cached = await _cache.GetAsync<MyData>(cacheKey);
        if (cached != null)
            return Ok(cached);
        
        // Get from database
        var data = await GetFromDatabase(id);
        
        // Cache for 30 minutes
        await _cache.SetAsync(cacheKey, data, TimeSpan.FromMinutes(30));
        
        return Ok(data);
    }
}
```

## Monitoring

- Health checks available at `/health`
- Logs include cache operation results
- Redis connection status visible in health check response
