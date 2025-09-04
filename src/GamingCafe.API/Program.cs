using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using GamingCafe.Data;
using GamingCafe.Data.Services;
using GamingCafe.API.Services;
using GamingCafe.API.Middleware;
using GamingCafe.API.Hubs;
using Hangfire;
using Hangfire.PostgreSql;
using GamingCafe.API.Filters;
using GamingCafe.API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
// OpenTelemetry instrumentation was attempted but reverted to avoid package conflicts; use internal /metrics endpoint instead if needed.
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using FluentValidation;
using FluentValidation.AspNetCore;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - Comment out until Serilog packages are installed
// builder.Host.UseSerilog((context, configuration) =>
//     configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version")
    );
})
.AddApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

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

// Bind rate limiting options from config
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
var rlOptions = new RateLimitingOptions();
builder.Configuration.GetSection("RateLimiting").Bind(rlOptions);
builder.Services.AddSingleton(rlOptions);

// Configure Hangfire for background jobs - Comment out until Hangfire packages are installed
// builder.Services.AddHangfire(config => 
//     config.UseInMemoryStorage());
// builder.Services.AddHangfireServer();

// Configure Enhanced Health Checks by registering dedicated IHealthCheck implementations
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "ef-core", "critical" })
    .AddCheck<ApplicationHealthCheck>("application", tags: new[] { "application", "system", "critical" })
    .AddCheck<EmailServiceHealthCheck>("email", tags: new[] { "email", "smtp", "external" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "redis", "cache", "optional" })
    .AddCheck<BackupServiceHealthCheck>("backup", tags: new[] { "backup", "optional" })
    .AddCheck<FileUploadServiceHealthCheck>("fileupload", tags: new[] { "fileupload", "optional" });

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

// Configure FluentValidation - use automatic MVC integration and register validators from this assembly
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Standardize invalid model state responses to RFC7807 ProblemDetails with validation details
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

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

// Register IHttpContextAccessor and in-memory cache for middleware and services
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Configure Hangfire: prefer PostgreSQL persistent storage when a Hangfire connection string is provided
var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire") ?? builder.Configuration.GetConnectionString("DefaultConnection");
var usePostgresHangfire = builder.Configuration.GetValue<bool?>("Hangfire:UsePostgres") ?? true;
var hangfireSchema = builder.Configuration["Hangfire:Schema"] ?? "public";

if (!string.IsNullOrEmpty(hangfireConnection) && usePostgresHangfire)
{
    // Check that the Hangfire tables exist in the target schema before wiring persistent storage.
    try
    {
        using var testConn = new Npgsql.NpgsqlConnection(hangfireConnection);
        testConn.Open();
        using var checkCmd = new Npgsql.NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = 'server');", testConn);
        checkCmd.Parameters.AddWithValue("schema", hangfireSchema);
        var existsObj = checkCmd.ExecuteScalar();
        var hasTables = existsObj is bool b && b;

        if (hasTables)
        {
            builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(hangfireConnection, new Hangfire.PostgreSql.PostgreSqlStorageOptions
            {
                SchemaName = hangfireSchema,
                PrepareSchemaIfNecessary = false
            }));
        }
        else
        {
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Program");
            logger.LogWarning("Hangfire tables not found in schema '{Schema}' — falling back to in-memory Hangfire.", hangfireSchema);
            builder.Services.AddHangfire(config => config.UseInMemoryStorage());
        }
    }
    catch (Exception ex)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Program");
        logger.LogWarning(ex, "Error checking Hangfire schema/tables — falling back to in-memory. Error: {Message}", ex.Message);
        builder.Services.AddHangfire(config => config.UseInMemoryStorage());
    }
}
else
{
    // Fallback to in-memory for development/testing
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
}

builder.Services.AddHangfireServer();

// Add custom services - register available implementations
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, CacheService>();
// builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ITwoFactorService, GamingCafe.Data.Services.TwoFactorService>();
// Register Data layer audit service first
builder.Services.AddScoped<GamingCafe.Data.Services.AuditService>();
// Then register API-level decorator for enrichment
builder.Services.AddScoped<IAuditService>(sp => new ApiAuditService(
    sp.GetRequiredService<GamingCafe.Data.Services.AuditService>(),
    sp.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddScoped<IBackupService, BackupService>();
// Database seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add deployment and monitoring services - Comment out until implemented
// builder.Services.AddScoped<IDeploymentValidationService, DeploymentValidationService>();
// builder.Services.AddScoped<IBackupMonitoringService, BackupMonitoringService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OpenTelemetry registration was intentionally removed to avoid package mismatches in this workspace.
// A simple /metrics endpoint is exposed later in this file for basic Prometheus-style scraping.

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

// Add global exception handling middleware
app.UseMiddleware<GamingCafe.API.Middleware.GlobalExceptionHandlingMiddleware>();

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

// Rate limiting: use ASP.NET Core built-in rate limiter configured from RateLimiting options (best practice)
app.UseRateLimiter();

// Increment allowed counter for successful requests (executed after the rate limiter)
app.Use(async (context, next) =>
{
    // If the response has already been set to 429 by the rate limiter, do not increment
    if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
    {
        RateLimitingMetrics.IncrementAllowed(1);
    }
    await next();
});

// Enable CORS
app.UseCors("LocalhostOnly");

// Add Hangfire Dashboard (secured)
var hangfireDashboardPath = builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire";
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments(hangfireDashboardPath), appBuilder =>
{
    appBuilder.UseHangfireDashboard(hangfireDashboardPath, new Hangfire.DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter() }
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Simple Prometheus-style /metrics endpoint for rate limiter counters
app.MapGet("/metrics", () =>
{
    var allowed = RateLimitingMetrics.GetAllowed();
    var rejected = RateLimitingMetrics.GetRejected();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# HELP gamingcafe_rate_limit_allowed Number of requests allowed by rate limiter");
    sb.AppendLine("# TYPE gamingcafe_rate_limit_allowed counter");
    sb.AppendLine($"gamingcafe_rate_limit_allowed {allowed}");
    sb.AppendLine("# HELP gamingcafe_rate_limit_rejected Number of requests rejected by rate limiter");
    sb.AppendLine("# TYPE gamingcafe_rate_limit_rejected counter");
    sb.AppendLine($"gamingcafe_rate_limit_rejected {rejected}");
    return Results.Text(sb.ToString(), "text/plain; version=0.0.4");
});

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
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database migration and seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}

// Run a one-time SMTP configuration validation at startup (non-blocking)
using (var startupScope = app.Services.CreateScope())
{
    try
    {
        var logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var emailService = startupScope.ServiceProvider.GetService<GamingCafe.Core.Interfaces.Services.IEmailService>();
        if (emailService != null)
        {
            var health = await emailService.TestConnectionAsync();
            if (!health.IsHealthy)
            {
                logger.LogWarning("SMTP health check failed at startup: {Error}", health.ErrorMessage);
            }
            else
            {
                logger.LogInformation("SMTP configuration validated at startup. Server: {Host}:{Port}", health.ServerInfo?.Host, health.ServerInfo?.Port);
            }
        }

        // Schedule backups with Hangfire if enabled in configuration
        var config = startupScope.ServiceProvider.GetRequiredService<IConfiguration>();
        var enableScheduled = bool.TryParse(config["Backup:EnableScheduled"], out var enabled) && enabled;
        if (enableScheduled)
        {
            var cronExpr = config["Backup:Cron"] ?? "0 2 * * *"; // default daily at 2 AM
            RecurringJob.AddOrUpdate("gamingcafe-scheduled-backup", () => startupScope.ServiceProvider.GetRequiredService<IBackupService>().CreateScheduledBackupAsync(), cronExpr);
            var logger2 = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger2.LogInformation("Scheduled recurring backup with cron: {Cron}", cronExpr);
        }
    }
    catch (Exception ex)
    {
        var logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Startup service checks failed");
    }
}

app.Run();
