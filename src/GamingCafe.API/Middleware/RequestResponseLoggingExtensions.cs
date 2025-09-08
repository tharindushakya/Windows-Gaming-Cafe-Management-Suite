using Microsoft.AspNetCore.Builder;

namespace GamingCafe.API.Middleware;

public static class RequestResponseLoggingExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
