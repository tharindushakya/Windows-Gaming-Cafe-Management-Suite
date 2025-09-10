using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;
using GamingCafe.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GamingCafe.API.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;

        public IdempotencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
            {
                await _next(context);
                return;
            }

            var key = keyValues.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                await _next(context);
                return;
            }

            // Short-circuit for already processed keys
            var db = context.RequestServices.GetRequiredService<GamingCafeContext>();
            var existing = await db.IdempotencyKeys.FindAsync(key);
            if (existing != null && existing.ProcessedAt != null)
            {
                // Replay stored response
                context.Response.StatusCode = existing.ResponseStatus ?? 200;
                if (!string.IsNullOrEmpty(existing.ResponseBody))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(existing.ResponseBody);
                }
                return;
            }

            // Reserve the key to prevent concurrent processing
            if (existing == null)
            {
                existing = new GamingCafe.Core.Models.IdempotencyKey { Key = key, Endpoint = context.Request.Path, ProcessedAt = null };
                db.IdempotencyKeys.Add(existing);
                await db.SaveChangesAsync();
            }

            // Capture response
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context);

            // Read response body
            memStream.Seek(0, SeekOrigin.Begin);
            var respBody = await new StreamReader(memStream).ReadToEndAsync();
            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            // Persist response snapshot and processed timestamp
            try
            {
                existing.ResponseStatus = context.Response.StatusCode;
                existing.ResponseBody = respBody.Length > 20000 ? respBody.Substring(0, 20000) : respBody; // truncate
                existing.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            catch
            {
                // Non-fatal: idempotency best-effort
            }
        }
    }
}
