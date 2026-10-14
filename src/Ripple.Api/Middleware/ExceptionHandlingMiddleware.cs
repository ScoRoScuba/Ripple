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
        SalesHistoryConflictException => (StatusCodes.Status409Conflict, "Conflicts With Tickets Sold"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
        // Thrown by ASP.NET when the request itself is malformed (e.g. a body that isn't valid JSON
        // for the request type): a client error, so keep the status it carries rather than a 500.
        BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad Request"),
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

    // A fault's message is internal (it can carry SQL, connection or stack details), so the client gets a
    // generic detail and the trace id to quote; the full exception is in the log under the same id.
    // For an unreadable body the useful part is the inner JSON error, which names the offending field
    // (e.g. "Path: $.time"); the outer message only says the body couldn't be read.
    private static string DetailFor(Exception exception, int statusCode) => exception switch
    {
        _ when statusCode >= StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
        BadHttpRequestException { InnerException: System.Text.Json.JsonException json } => json.Message,
        _ => exception.Message
    };

    private static Task WriteProblemAsync(HttpContext context, Exception exception, int statusCode, string title)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title,
                status = statusCode,
                detail = DetailFor(exception, statusCode),
                traceId = context.TraceIdentifier
            },
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json");
    }
}
