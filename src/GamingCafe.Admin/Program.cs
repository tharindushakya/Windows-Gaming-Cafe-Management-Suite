using GamingCafe.Admin.Components;
using GamingCafe.Admin.Services;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for API communication
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure Entity Framework with DbContextFactory for thread safety
builder.Services.AddDbContextFactory<GamingCafeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Admin-specific services
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<AdminNotificationService>();
builder.Services.AddScoped<AuthenticationStateProvider, AdminAuthenticationStateProvider>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add cookie authentication for admin panel sessions
builder.Services.AddAuthentication("AdminCookies")
    .AddCookie("AdminCookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "GamingCafe.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy =>
        policy.RequireRole("Admin", "Staff"));
});

// Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Redirect the site root to the dashboard page explicitly to avoid 404s from direct GET '/'
app.MapGet("/", context =>
{
    context.Response.Redirect("/dashboard");
    return Task.CompletedTask;
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hubs
app.MapHub<AdminHub>("/adminhub");

// Debug endpoint to receive client-side debug events (beacon or POST)
app.MapPost("/debug/event", async (HttpContext ctx, ILogger<Program> logger) =>
{
    try
    {
        string body;
        using (var reader = new StreamReader(ctx.Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            logger.LogInformation("[Admin Debug Event] {Payload}", body);
        }

        ctx.Response.StatusCode = 204; // no content
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 500;
        logger.LogError(ex, "Error processing debug event");
    }
});

app.Run();
