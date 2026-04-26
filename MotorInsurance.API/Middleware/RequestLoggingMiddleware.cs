using System.Diagnostics;

namespace MotorInsurance.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;
            var user = context.User?.Identity?.Name ?? "anonymous";

            _logger.LogInformation("→ {Method} {Path} | User: {User}", method, path, user);

            await _next(context);

            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = stopwatch.ElapsedMilliseconds;

            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level, "← {StatusCode} {Method} {Path} | {Elapsed}ms",
                statusCode, method, path, elapsed);
        }
    }
}
