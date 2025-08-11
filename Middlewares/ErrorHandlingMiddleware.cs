using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
                // If the response has already started, we can't write our JSON envelope safely
                if (context.Response.HasStarted)
                {
                    _logger.LogError(ex, "Unhandled exception after response started.");
                    throw;
                }

                _logger.LogError(ex, "Unhandled exception occurred.");

                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var payload = new { error = "Internal server error." };
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }
    }
}
