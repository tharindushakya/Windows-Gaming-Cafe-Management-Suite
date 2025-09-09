using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace GamingCafe.API.Middleware
{
    // Adds or reads an X-Correlation-ID header, stores it on the Activity and response header,
    // and enriches Serilog scope with CorrelationId, TraceId and SpanId for log correlation.
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string HeaderName = "X-Correlation-ID";

        public CorrelationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = System.Guid.NewGuid().ToString();
            }

            // Ensure Activity has the correlation id in baggage so it flows to traces
            var activity = Activity.Current ?? new Activity("CorrelationActivity");
            if (Activity.Current == null)
            {
                activity.Start();
            }

            activity?.AddBaggage("correlation_id", correlationId);

            // Enrich Serilog context with correlation id and any available trace/span ids
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", activity?.TraceId.ToString() ?? string.Empty))
            using (LogContext.PushProperty("SpanId", activity?.SpanId.ToString() ?? string.Empty))
            {
                context.Response.OnStarting(() =>
                {
                    // Use the header dictionary indexer to set or overwrite the header safely
                    context.Response.Headers[HeaderName] = correlationId;
                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }
    }
}
