using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

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
        var (statusCode, message, details) = exception switch
        {
            ValidationException validationEx => (StatusCodes.Status400BadRequest, validationEx.Message, validationEx.Errors?.ToArray() as object),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access", null),
            NotFoundException notFoundEx => (StatusCodes.Status404NotFound, notFoundEx.Message, null),
            BusinessRuleException businessEx => (StatusCodes.Status400BadRequest, businessEx.Message, null),
            PaymentException paymentEx => (StatusCodes.Status402PaymentRequired, paymentEx.Message, null),
            ConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by another user. Please refresh and try again.", null),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "The request timed out. Please try again.", null),
            _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred", null)
        };

        // Build RFC7807 ProblemDetails
        var prob = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = message,
            Status = statusCode,
            Instance = context.Request.Path
        };

        if (details != null)
        {
            prob.Extensions["details"] = details;
        }

        // Include correlation id (if present) to help trace issues end-to-end
        var correlationId = context.Request.Headers.ContainsKey("X-Correlation-ID") ? context.Request.Headers["X-Correlation-ID"].ToString() : context.TraceIdentifier;
        prob.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var jsonResponse = JsonSerializer.Serialize(prob, new JsonSerializerOptions
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
