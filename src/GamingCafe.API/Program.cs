using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using GamingCafe.Data;
using GamingCafe.Data.Services;
using System.Threading.RateLimiting;
using GamingCafe.API.Services;
using GamingCafe.API.Middleware;
using GamingCafe.API.Hubs;
using Hangfire;
using Hangfire.PostgreSql;
using GamingCafe.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
// OpenTelemetry instrumentation was attempted but reverted to avoid package conflicts; use internal /metrics endpoint instead if needed.
using FluentValidation;
using FluentValidation.AspNetCore;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Services;
using StackExchange.Redis;
using GamingCafe.Core.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog: initialize a default logger and attach it to the Host. This ensures
// structured diagnostics are available early. Configuration will override these defaults.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: Serilog.RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

// NOTE: DataProtection key persistence is configured later below so we can prefer
// a centralized store (Redis) when available. This avoids non-sticky local file
// storage which breaks auth in scaled multi-instance deployments.

// ...existing code...

// Add services to the container.
builder.Services.AddControllers();

// Configure API Versioning and API explorer for Swagger integration
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version")
    );
});

// Note: API explorer for per-version Swagger docs is intentionally omitted to avoid extra package dependencies.
// We still register basic API versioning above; Swagger will expose a default v1 doc.

// Configure Entity Framework
builder.Services.AddDbContext<GamingCafeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Redis Cache (register the distributed cache client) but avoid connecting synchronously
try
{
    var redisConfig = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    // Register IDistributedCache implementation using StackExchange Redis (will not create a ConnectionMultiplexer here)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfig;
        options.InstanceName = "GamingCafe";
    });
}
catch (Exception ex)
{
    Log.Warning(ex, "Redis setup failed during registration. Application will run without Redis cache.");
}

// Respect DataProtection:UseRedis in configuration. Default to true to prefer Redis in multi-instance.
var useRedisForDataProtection = builder.Configuration.GetValue<bool?>("DataProtection:UseRedis") ?? false;

// Default to filesystem persistence now (safe dev default). For production, enable DataProtection:UseRedis = true
// and configure DataProtection:Redis:Connection (or ConnectionStrings:Redis) and DataProtection:Redis:Key.
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysDir);

// Prepare the data protection builder so we can choose persistence at startup
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("GamingCafe");

var dpRedisKey = builder.Configuration["DataProtection:Redis:Key"] ?? "GamingCafe-DataProtection-Keys";

if (useRedisForDataProtection)
{
    // Read connection string from DataProtection:Redis:Connection or ConnectionStrings:Redis
    var dpRedisConnection = builder.Configuration["DataProtection:Redis:Connection"] ?? builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(dpRedisConnection))
    {
    Log.Warning("DataProtection:UseRedis is true but no Redis connection string was found; falling back to file keys.");
        dataProtectionBuilder.PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir));
    }
    else
    {
        try
        {
            // Create and register ConnectionMultiplexer for application lifetime
            var mux = ConnectionMultiplexer.Connect(dpRedisConnection);
            builder.Services.AddSingleton<IConnectionMultiplexer>(mux);

            // Persist DataProtection keys to StackExchange.Redis using the configured key
                dataProtectionBuilder.PersistKeysToStackExchangeRedis(mux, dpRedisKey);
                // Protect keys at rest: prefer certificate (thumbprint or PFX), fall back to DPAPI on Windows
                var certThumb = builder.Configuration["DataProtection:Certificate:Thumbprint"];
                var pfxPath = builder.Configuration["DataProtection:Certificate:PfxPath"];
                var pfxPassword = builder.Configuration["DataProtection:Certificate:PfxPassword"];
                if (!string.IsNullOrWhiteSpace(certThumb))
                {
                    try
                    {
                        var storeName = builder.Configuration["DataProtection:Certificate:StoreName"] ?? "My";
                        var storeLocationStr = builder.Configuration["DataProtection:Certificate:StoreLocation"] ?? "CurrentUser";
                        var storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocationStr, true);
                        using var store = new X509Store(storeName, storeLocation);
                        store.Open(OpenFlags.ReadOnly);
                        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certThumb, validOnly: false);
                        if (certs.Count > 0)
                        {
                            var cert = certs[0];
                            dataProtectionBuilder.ProtectKeysWithCertificate(cert);
                        }
                        else
                        {
                            Log.Warning("DataProtection certificate with thumbprint {thumb} not found in store {store}\\{location}. Falling back to DPAPI where available.", certThumb, storeName, storeLocationStr);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error loading DataProtection certificate by thumbprint; falling back to DPAPI if available.");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(pfxPath) && File.Exists(pfxPath))
                {
                    try
                    {
                        var pwd = pfxPassword ?? string.Empty;
                        var cert = new X509Certificate2(pfxPath, pwd, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                        dataProtectionBuilder.ProtectKeysWithCertificate(cert);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to load PFX for DataProtection key encryption; falling back to DPAPI where available.");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // DPAPI is available on Windows
                    dataProtectionBuilder.ProtectKeysWithDpapi();
                }
                else
                {
                    Log.Warning("No XML encryptor configured for DataProtection keys. Configure a certificate (DataProtection:Certificate) or enable platform protection to avoid storing keys unencrypted.");
                }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Redis for DataProtection; falling back to filesystem keys.");
            dataProtectionBuilder.PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir));
        }
    }
}
else
{
    // Development or explicit opt-out: use local filesystem keys
    dataProtectionBuilder.PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir));
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

// Add Authorization and register named policies for policy-based authorization
builder.Services.AddAuthorization(options =>
{
    // Simple role-based policy for administrators
    options.AddPolicy(PolicyNames.RequireAdmin, policy => policy.RequireRole(RoleNames.Admin));

    // Manager or Admin can be used for mid-level operations
    options.AddPolicy(PolicyNames.RequireManagerOrAdmin, policy => policy.RequireRole(RoleNames.Admin, RoleNames.Manager));

    // Scope/claim based policy example for station management operations
    options.AddPolicy(PolicyNames.RequireStationScope, policy => policy.RequireClaim("scope", "stations.manage"));

    // Owner-or-admin policy uses an IAuthorizationHandler registered below
    options.AddPolicy(PolicyNames.RequireOwnerOrAdmin, policy => policy.AddRequirements(new GamingCafe.API.Authorization.OwnershipRequirement()));
});

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, GamingCafe.API.Authorization.OwnershipHandler>();

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

// Add a default/global rate limiter so app.UseRateLimiter() can operate without throwing
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "global", factory: _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

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
            // Use the newer overload to avoid obsolete API
            builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(_ => { }, new Hangfire.PostgreSql.PostgreSqlStorageOptions
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

// Background task queue and hosted worker for reliable fire-and-forget work (emails, verification, etc.)
var queueCapacity = builder.Configuration.GetValue<int?>("BackgroundQueue:Capacity") ?? 1000;
builder.Services.AddSingleton<GamingCafe.Core.Interfaces.Background.IBackgroundTaskQueue>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<GamingCafe.API.Background.BackgroundTaskQueue>>();
    return new GamingCafe.API.Background.BackgroundTaskQueue(logger, queueCapacity);
});
builder.Services.AddHostedService<GamingCafe.API.Background.QueuedHostedService>();

// Optional persistent scheduled job store (hybrid scheduling): register if EF DbContext is available and enabled in config
var enablePersistentScheduling = builder.Configuration.GetValue<bool?>("BackgroundQueue:PersistentSchedulingEnabled") ?? false;
if (enablePersistentScheduling)
{
    builder.Services.AddScoped<GamingCafe.Core.Interfaces.Background.IScheduledJobStore, GamingCafe.Data.Repositories.ScheduledJobStore>();
}

// Respect X-Forwarded-* headers when behind a proxy/load balancer so RemoteIpAddress reflects client IP
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear known networks/proxies so middleware will accept headers from any trusted reverse proxy configured externally.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add deployment and monitoring services - Comment out until implemented
// builder.Services.AddScoped<IDeploymentValidationService, DeploymentValidationService>();
// builder.Services.AddScoped<IBackupMonitoringService, BackupMonitoringService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with a single consolidated registration.
builder.Services.AddSwaggerGen(opts =>
{
    // Register a default v1 document
    opts.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "GamingCafe API v1", Version = "v1" });

    // Resolve conflicting actions by taking the first description (keeps behavior from before but avoids double-registration)
    opts.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// OpenTelemetry registration was intentionally removed to avoid package mismatches in this workspace.
// A simple /metrics endpoint is exposed later in this file for basic Prometheus-style scraping.

var app = builder.Build();
// Use forwarded headers middleware early so downstream middleware (rate limiter, logging) sees the correct client IP
app.UseForwardedHeaders();

// Conditional request/response body logging middleware (config-gated, safe defaults). Place early so it sees real request/response but after forwarded headers.
try
{
    var config = app.Services.GetService<IConfiguration>();
    var enabled = config?.GetValue<bool?>("Logging:RequestResponse:Enabled") ?? false;
    if (enabled)
    {
        app.UseRequestResponseLogging();
        Log.Information("Request/Response logging middleware enabled");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Error while registering RequestResponseLogging middleware");
}

// Startup verification: if Redis is configured for DataProtection, warn if no keys are present
if (useRedisForDataProtection)
{
    try
    {
        var config = app.Services.GetService<IConfiguration>();
        var dpRedisKeyCheck = config?["DataProtection:Redis:Key"] ?? "GamingCafe-DataProtection-Keys";
        var mux = app.Services.GetService<IConnectionMultiplexer>();
        if (mux == null)
        {
            var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
            logger?.LogWarning("DataProtection is configured to use Redis but no ConnectionMultiplexer is registered. Keys may be missing.");
        }
        else
        {
            var db = mux.GetDatabase();
            var type = db.KeyType(dpRedisKeyCheck);
            if (type == RedisType.None)
            {
                var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogWarning("DataProtection: Redis key '{key}' does not exist or contains no keys. Ensure migration completed and Redis is reachable.", dpRedisKeyCheck);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
        logger?.LogWarning(ex, "Failed to verify DataProtection keys in Redis at startup.");
    }
}

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
    // Background queue metrics
    try
    {
        var queue = app.Services.GetService<GamingCafe.Core.Interfaces.Background.IBackgroundTaskQueue>();
        if (queue != null)
        {
            var lens = queue.GetQueueLengths();
            sb.AppendLine("# HELP gamingcafe_background_queue_high_length Approximate number of items in high priority queue");
            sb.AppendLine("# TYPE gamingcafe_background_queue_high_length gauge");
            sb.AppendLine($"gamingcafe_background_queue_high_length {lens.High}");
            sb.AppendLine("# HELP gamingcafe_background_queue_normal_length Approximate number of items in normal priority queue");
            sb.AppendLine("# TYPE gamingcafe_background_queue_normal_length gauge");
            sb.AppendLine($"gamingcafe_background_queue_normal_length {lens.Normal}");
            sb.AppendLine("# HELP gamingcafe_background_queue_low_length Approximate number of items in low priority queue");
            sb.AppendLine("# TYPE gamingcafe_background_queue_low_length gauge");
            sb.AppendLine($"gamingcafe_background_queue_low_length {lens.Low}");

            var failures = queue.GetFailureCount();
            sb.AppendLine("# HELP gamingcafe_background_task_failures Total background task failures since process start");
            sb.AppendLine("# TYPE gamingcafe_background_task_failures counter");
            sb.AppendLine($"gamingcafe_background_task_failures {failures}");
        }
    }
    catch { }
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

// Apply EF Core migrations under an advisory lock to avoid race conditions when multiple instances start
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    // Only run migrations when there are pending migrations
    try
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            // Use Postgres advisory lock to serialize migrations across instances
            try
            {
                await using var conn = new Npgsql.NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                // Use a fixed lock key; choose a value unique to this application
                const long advisoryLockKey1 = 92233720368547758; // large arbitrary number
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT pg_advisory_lock(@key)";
                    cmd.Parameters.AddWithValue("key", advisoryLockKey1);
                    await cmd.ExecuteScalarAsync();
                }

                try
                {
                    Log.Information("Acquired advisory lock; applying pending EF migrations...");
                    await context.Database.MigrateAsync();
                    Log.Information("Database migrations applied successfully.");
                }
                finally
                {
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT pg_advisory_unlock(@key)";
                        cmd.Parameters.AddWithValue("key", advisoryLockKey1);
                        await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to acquire advisory lock or apply migrations; another instance may be migrating. Proceeding without applying migrations.");
            }
        }

        // Seed the database if environment is Development
        if (app.Environment.IsDevelopment())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error while checking/applying migrations at startup");
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

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
