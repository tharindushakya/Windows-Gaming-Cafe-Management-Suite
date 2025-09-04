using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using System.Text;
using GamingCafe.Data;
using GamingCafe.API.Services;
using GamingCafe.API.Hubs;
using GamingCafe.API.Middleware;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Serilog;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.InMemory;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

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
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "GamingCafe";
});

// Configure Hangfire for background jobs
builder.Services.AddHangfire(config => 
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Configure Health Checks (Enhanced monitoring - basic for now)
builder.Services.AddHealthChecks();

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

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

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

// Add custom services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

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

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

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

// Add Hangfire Dashboard (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<GameCafeHub>("/gamecafehub");

// Map Health Check endpoints
app.MapHealthChecks("/health");

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
            var seeder = new DatabaseSeeder(context, 
                scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>());
            await seeder.SeedAsync();
            logger.LogInformation("Database migration and seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}

app.Run();
