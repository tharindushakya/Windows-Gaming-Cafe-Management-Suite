using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using GamingCafe.Data;
using GamingCafe.API.Services;
using GamingCafe.API.Hubs;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - Comment out until Serilog packages are installed
// builder.Host.UseSerilog((context, configuration) =>
//     configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();

// Configure API Versioning - Comment out until versioning packages are installed
// builder.Services.AddApiVersioning(options =>
// {
//     options.DefaultApiVersion = new ApiVersion(1, 0);
//     options.AssumeDefaultVersionWhenUnspecified = true;
//     options.ApiVersionReader = ApiVersionReader.Combine(
//         new QueryStringApiVersionReader("version"),
//         new HeaderApiVersionReader("X-Version")
//     );
// })
// .AddApiExplorer(setup =>
// {
//     setup.GroupNameFormat = "'v'VVV";
//     setup.SubstituteApiVersionInUrl = true;
// });

// Configure Entity Framework
builder.Services.AddDbContext<GamingCafeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Redis Cache
try
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.InstanceName = "GamingCafe";
    });

    // Register Redis ConnectionMultiplexer for advanced operations
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        try
        {
            var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        }
        catch (Exception ex)
        {
            var logger = provider.GetService<ILogger<Program>>();
            logger?.LogWarning(ex, "Failed to connect to Redis at startup. Cache will fall back to in-memory.");
            return null!; // Return null, CacheService will handle gracefully
        }
    });
}
catch (Exception ex)
{
    // If Redis setup fails completely, log and continue without Redis
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogWarning(ex, "Redis setup failed. Application will run without Redis cache.");
}

// Configure Hangfire for background jobs - Comment out until Hangfire packages are installed
// builder.Services.AddHangfire(config => 
//     config.UseInMemoryStorage());
// builder.Services.AddHangfireServer();

// Configure Enhanced Health Checks with Best Practices
builder.Services.AddHealthChecks()
    // Basic Database Health Check using EF Core
    .AddCheck("database", () =>
    {
        try
        {
            // This is acceptable for health checks as it's a simple connectivity test
            #pragma warning disable ASP0000
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            #pragma warning restore ASP0000
            var context = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();
            return context.Database.CanConnect() ? HealthCheckResult.Healthy("Database is accessible") 
                                                 : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }, tags: new[] { "database", "ef-core", "critical" })
    
    // Basic Application Health Check
    .AddCheck("application", () =>
    {
        try
        {
            var memoryUsed = GC.GetTotalMemory(false);
            return memoryUsed < 1_000_000_000 // 1GB threshold
                ? HealthCheckResult.Healthy($"Application healthy, memory usage: {memoryUsed / 1024 / 1024} MB")
                : HealthCheckResult.Degraded($"High memory usage: {memoryUsed / 1024 / 1024} MB");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Application health check failed", ex);
        }
    }, tags: new[] { "application", "system", "critical" })
    
    // Email Service Health Check
    .AddCheck("email", () =>
    {
        try
        {
            #pragma warning disable ASP0000
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            #pragma warning restore ASP0000
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var healthResult = emailService.TestConnectionAsync().GetAwaiter().GetResult();
            
            return healthResult.IsHealthy 
                ? HealthCheckResult.Healthy($"Email service healthy - Response time: {healthResult.ResponseTime.TotalMilliseconds:F0}ms")
                : HealthCheckResult.Unhealthy($"Email service unhealthy: {healthResult.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Email health check failed", ex);
        }
    }, tags: new[] { "email", "smtp", "external" })
    
    // Redis Cache Health Check
    .AddCheck("redis", () =>
    {
        try
        {
            #pragma warning disable ASP0000
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            #pragma warning restore ASP0000
            var redis = scope.ServiceProvider.GetService<IConnectionMultiplexer>();
            
            if (redis == null)
            {
                return HealthCheckResult.Degraded("Redis not configured - falling back to in-memory cache");
            }
            
            if (!redis.IsConnected)
            {
                return HealthCheckResult.Degraded("Redis not connected - falling back to in-memory cache");
            }
            
            var database = redis.GetDatabase();
            var testKey = "health_check_" + Guid.NewGuid();
            var testValue = "test";
            
            // Test set and get operations
            database.StringSet(testKey, testValue);
            var retrievedValue = database.StringGet(testKey);
            database.KeyDelete(testKey);
            
            return retrievedValue == testValue 
                ? HealthCheckResult.Healthy("Redis is connected and responding")
                : HealthCheckResult.Degraded("Redis operations failed - falling back to in-memory cache");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Redis unavailable - falling back to in-memory cache: {ex.Message}");
        }
    }, tags: new[] { "redis", "cache", "optional" });

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Configure FluentValidation - Comment out until FluentValidation packages are installed
// builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// builder.Services.AddFluentValidationAutoValidation();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for localhost development
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5000", "http://localhost:7000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add custom services - Comment out until interfaces and implementations are created
builder.Services.AddScoped<IEmailService, EmailService>();
// builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, CacheService>();
// builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
// builder.Services.AddScoped<IBackupService, BackupService>();

// Add deployment and monitoring services - Comment out until implemented
// builder.Services.AddScoped<IDeploymentValidationService, DeploymentValidationService>();
// builder.Services.AddScoped<IBackupMonitoringService, BackupMonitoringService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Force HTTPS in production
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Add global exception handling middleware - Comment out until middleware is created
// app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

// Enable CORS
app.UseCors("LocalhostOnly");

// Add Hangfire Dashboard (Development only) - Comment out until Hangfire is installed
// if (app.Environment.IsDevelopment())
// {
//     app.UseHangfireDashboard("/hangfire");
// }

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<GameCafeHub>("/gamecafehub");

// Map Health Check endpoints with detailed responses
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.ToString(),
                tags = x.Value.Tags
            }),
            duration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Simple health check for load balancer
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks, just returns healthy if app is running
});

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();
    
    // Ensure database is created
    context.Database.EnsureCreated();
    
    // Apply any pending migrations
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
    
    // Seed the database if environment is Development
    if (app.Environment.IsDevelopment())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Database seeding - Comment out until DatabaseSeeder is implemented
            // var seeder = new DatabaseSeeder(context, 
            //     scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>());
            // await seeder.SeedAsync();
            logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}

app.Run();
