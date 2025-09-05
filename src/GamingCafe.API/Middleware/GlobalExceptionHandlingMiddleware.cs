using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GamingCafe.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. Request: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (HttpStatusCode.BadRequest, validationEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message),
            BusinessRuleException businessEx => (HttpStatusCode.BadRequest, businessEx.Message),
            PaymentException paymentEx => (HttpStatusCode.PaymentRequired, paymentEx.Message),
            ConcurrencyException => (HttpStatusCode.Conflict, "The record was modified by another user. Please refresh and try again."),
            TimeoutException => (HttpStatusCode.RequestTimeout, "The request timed out. Please try again."),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        // Include detailed exception string in development environment for easier debugging
        string[]? details = exception switch
        {
            ValidationException validationEx => validationEx.Errors?.ToArray(),
            _ => null
        };

        var env = context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
        if (env != null && env.IsDevelopment())
        {
            // append full exception for dev only
            details = details == null ? new[] { exception.ToString() } : details.Concat(new[] { exception.ToString() }).ToArray();
        }

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = details,
            TraceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

// Custom exception types
public class ValidationException : Exception
{
    public IEnumerable<string>? Errors { get; }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityName, object key) : base($"{entityName} with key {key} was not found") { }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
    public PaymentException(string message, Exception innerException) : base(message, innerException) { }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException() : base("The record was modified by another user") { }
    public ConcurrencyException(string message) : base(message) { }
}

// Error response model
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[]? Details { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
