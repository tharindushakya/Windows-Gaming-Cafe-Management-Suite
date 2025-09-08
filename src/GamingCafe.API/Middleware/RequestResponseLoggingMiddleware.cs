using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace GamingCafe.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly IConfiguration _config;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Global settings
        var enabledGlobal = _config.GetValue<bool?>("Logging:RequestResponse:Enabled") ?? false;
        var maxBodyGlobal = _config.GetValue<int?>("Logging:RequestResponse:MaxBodySize") ?? 4096;
        // sampling rate in 0..1 (e.g. 0.01 == 1%)
        var sampleRateGlobal = _config.GetValue<double?>("Logging:RequestResponse:SamplingRate") ?? 0.01;

        var path = context.Request.Path.ToString();

        // Include paths: if specified, only these prefixes will be considered for logging
        var include = _config.GetSection("Logging:RequestResponse:IncludePaths").Get<string[]>() ?? Array.Empty<string>();
        if (include.Length > 0 && !include.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Per-path overrides (longest prefix wins)
        var pathSections = _config.GetSection("Logging:RequestResponse:Paths").GetChildren().ToList();
        IConfigurationSection? matched = null;
        if (pathSections.Count > 0)
        {
            matched = pathSections.OrderByDescending(s => s.Key.Length).FirstOrDefault(s => path.StartsWith(s.Key, StringComparison.OrdinalIgnoreCase));
        }

        var enabled = matched?.GetValue<bool?>("Enabled") ?? enabledGlobal;
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var maxBody = matched?.GetValue<int?>("MaxBodySize") ?? maxBodyGlobal;
        var sampleRate = matched?.GetValue<double?>("SamplingRate") ?? sampleRateGlobal;

        // Sampling decision (Random.Shared is thread-safe)
        try
        {
            if (sampleRate <= 0.0 || sampleRate >= 1.0)
            {
                // if 0 => disabled, if >=1 => always sample
            }
        }
        catch { }
        if (sampleRate > 0 && sampleRate < 1 && Random.Shared.NextDouble() > sampleRate)
        {
            await _next(context);
            return;
        }

        // Log request
        string requestBody = "";
        try
        {
            if (context.Request.ContentLength > 0 && IsTextBasedContentType(context.Request.ContentType))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var toRead = (int)Math.Min(maxBody, context.Request.ContentLength ?? maxBody);
                var buffer = new char[toRead];
                var read = await reader.ReadBlockAsync(buffer, 0, toRead);
                requestBody = new string(buffer, 0, read);
                if ((context.Request.ContentLength ?? 0) > read)
                {
                    requestBody += "...TRUNCATED...";
                }
                context.Request.Body.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request body for logging");
        }

        // Capture the response
        var originalBody = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string responseText = "";
        try
        {
            if (IsTextBasedContentType(context.Response.ContentType))
            {
                using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var buffer = new char[Math.Min(maxBody, (int)responseBody.Length)];
                var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                responseText = new string(buffer, 0, read);
                if (responseBody.Length > read)
                {
                    responseText += "...TRUNCATED...";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read response body for logging");
        }

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        // Redact sensitive headers and PII in bodies
        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var sensitiveHeaderKeys = new[] { "Authorization", "Cookie", "Set-Cookie" };
        foreach (var k in sensitiveHeaderKeys)
        {
            if (headers.ContainsKey(k)) headers[k] = "REDACTED";
        }

        // Redact PII in bodies using regexes
        requestBody = RedactPII(requestBody);
        responseText = RedactPII(responseText);

        _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms\nRequestHeaders: {@ReqHeaders}\nRequestBody: {ReqBody}\nResponseBody: {RespBody}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            headers,
            string.IsNullOrEmpty(requestBody) ? "<empty>" : requestBody,
            string.IsNullOrEmpty(responseText) ? "<empty>" : responseText);
    }

    private static readonly Regex EmailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    // Very permissive credit card regex: 13-19 digits optionally separated by spaces or dashes
    private static readonly Regex CcRegex = new Regex(@"(?<!\d)(?:\d[ -]*?){13,19}(?!\d)", RegexOptions.Compiled);

    private static string RedactPII(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        try
        {
            var step1 = EmailRegex.Replace(input, "[REDACTED-EMAIL]");
            var step2 = CcRegex.Replace(step1, "[REDACTED-CC]");
            return step2;
        }
        catch { return input; }
    }

    private static bool IsTextBasedContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        contentType = contentType.ToLowerInvariant();
        return contentType.StartsWith("text/") || contentType.Contains("json") || contentType.Contains("xml") || contentType.Contains("form");
    }
}
