using Sixram.Api.DTOs;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = MapException(exception);

        if (exception is ApiException)
        {
            _logger.LogWarning(
                exception,
                "Handled API exception for {Method} {Path} with status {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                statusCode);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
            TraceId = context.TraceIdentifier,
            Errors = errors
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static (int StatusCode, string Title, IReadOnlyDictionary<string, string[]>? Errors) MapException(Exception exception)
    {
        return exception switch
        {
            ApiException apiException => (apiException.StatusCode, "Request failed", apiException.Errors),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", null)
        };
    }
}
