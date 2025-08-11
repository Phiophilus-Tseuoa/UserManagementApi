using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middlewares
{
    public class TokenAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenAuthenticationMiddleware> _logger;
        private readonly string? _expectedToken;
        private static readonly string[] BypassPrefixes = new[] { "/swagger", "/health", "/favicon.ico" };

        public TokenAuthenticationMiddleware(RequestDelegate next, ILogger<TokenAuthenticationMiddleware> logger, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _expectedToken = config["Auth:ApiToken"]; // e.g., set in appsettings or environment
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Allow preflight
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Bypass certain paths (optional, helpful for Swagger)
            var path = context.Request.Path.Value ?? string.Empty;
            if (BypassPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Enforce presence of configured token
            if (string.IsNullOrWhiteSpace(_expectedToken))
            {
                _logger.LogWarning("Auth token is not configured; denying all non-bypassed requests.");
                await WriteUnauthorizedAsync(context);
                return;
            }

            // Parse Authorization: Bearer <token>
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                await WriteUnauthorizedAsync(context);
                return;
            }

            var raw = authHeader.ToString();
            const string scheme = "Bearer ";
            if (!raw.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                await WriteUnauthorizedAsync(context);
                return;
            }

            var token = raw.Substring(scheme.Length).Trim();
            if (!string.Equals(token, _expectedToken, StringComparison.Ordinal))
            {
                await WriteUnauthorizedAsync(context);
                return;
            }

            // Optionally stash token or user identity info
            context.Items["Token"] = token;

            await _next(context);
        }

        private static async Task WriteUnauthorizedAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            context.Response.Headers["WWW-Authenticate"] = "Bearer";
            var payload = new { error = "Unauthorized" };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
