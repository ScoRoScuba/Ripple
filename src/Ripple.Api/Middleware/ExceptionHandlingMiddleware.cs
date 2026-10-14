using Ripple.Domain.Exceptions;

namespace Ripple.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title) = MapException(ex);
            LogException(context, ex, statusCode);
            await WriteProblemAsync(context, ex, statusCode, title);
        }
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        EventNotFoundException or PricingTierNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
        InsufficientCapacityException => (StatusCodes.Status409Conflict, "Insufficient Capacity"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
    };

    // Handled domain exceptions are expected outcomes: Warning, no stack trace.
    // Anything unmapped is a fault: Error, with the full exception.
    private void LogException(HttpContext context, Exception exception, int statusCode)
    {
        var isFault = statusCode >= StatusCodes.Status500InternalServerError;

        logger.Log(isFault ? LogLevel.Error : LogLevel.Warning, isFault ? exception : null,
            "[ExceptionHandlingMiddleware]:[InvokeAsync] - {ExceptionType} ({ExceptionMessage}) for {Method} {Path}{QueryString} returned {StatusCode} (TraceId {TraceId})",
            exception.GetType().Name,
            exception.Message,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.Value,
            statusCode,
            context.TraceIdentifier);
    }

    private static Task WriteProblemAsync(HttpContext context, Exception exception, int statusCode, string title)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title,
                status = statusCode,
                detail = exception.Message
            },
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json");
    }
}
