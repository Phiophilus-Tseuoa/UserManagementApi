namespace UserManagementAPI.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";

            try
            {
                await _next(context);
            }
            finally
            {
                var statusCode = context.Response?.StatusCode;
                _logger.LogInformation("HTTP {Method} {Path} => {StatusCode}", method, path, statusCode);
            }
        }
    }
}
