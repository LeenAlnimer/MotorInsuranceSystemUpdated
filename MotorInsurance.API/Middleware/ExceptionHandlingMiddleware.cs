using System.Net;
using System.Text.Json;

namespace MotorInsurance.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started — cannot rewrite error response");
                    throw;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errorDesc) = exception switch
            {
                ArgumentException or ArgumentNullException =>
                    (HttpStatusCode.BadRequest, exception.Message),

                KeyNotFoundException =>
                    (HttpStatusCode.NotFound, exception.Message),

                UnauthorizedAccessException =>
                    (HttpStatusCode.Forbidden, exception.Message),

                InvalidOperationException =>
                    (HttpStatusCode.Conflict, exception.Message),

                NotSupportedException =>
                    (HttpStatusCode.BadRequest, exception.Message),

                TimeoutException =>
                    (HttpStatusCode.RequestTimeout, "The request timed out"),

                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };

            var level = statusCode == HttpStatusCode.InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning;

            _logger.Log(level, exception, "{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                errorCode = (int)statusCode,
                errorDesc
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
