using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamingCafe.API.Middleware;

public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;

    public ProblemDetailsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            var correlationId = context.Response.Headers.ContainsKey("X-Correlation-ID") ? context.Response.Headers["X-Correlation-ID"].ToString() : (context.Request.Headers.ContainsKey("X-Correlation-ID") ? context.Request.Headers["X-Correlation-ID"].ToString() : traceId);

            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7807",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            // Add correlation id for client troubleshooting
            problem.Extensions["correlationId"] = correlationId;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        }
    }
}
