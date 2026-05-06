namespace Sixram.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
            headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            headers.TryAdd("Cross-Origin-Resource-Policy", "same-site");
            headers.TryAdd("Permissions-Policy", "camera=(), geolocation=(), microphone=()");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
