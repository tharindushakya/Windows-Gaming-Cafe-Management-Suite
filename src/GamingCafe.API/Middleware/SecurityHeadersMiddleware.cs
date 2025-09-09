using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GamingCafe.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content-Security-Policy: conservative default, adjust per app needs
        var csp = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; img-src 'self' data:; connect-src 'self' https://api.ipify.org";
        context.Response.Headers["Content-Security-Policy"] = csp;

        // Permissions-Policy (formerly Feature-Policy) - restrict powerful features
        var permissions = "geolocation=(), microphone=(), camera=(), payment=()";
        context.Response.Headers["Permissions-Policy"] = permissions;

        // Keep HSTS handled separately in Program.cs for production only

        await _next(context);
    }
}
