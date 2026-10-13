using Ripple.Reports;
using Ripple.Domain.Exceptions;

namespace Ripple.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/sales", async (IReportService service) =>
            Results.Ok(await service.GetAllEventsReportAsync()))
            .WithName("GetAllSalesReports")
            .WithSummary("Get ticket sales summary for all events");

        group.MapGet("/sales/{eventId:guid}", async (Guid eventId, IReportService service) =>
        {
            var report = await service.GetEventReportAsync(eventId);
            return report is null
                ? Results.NotFound(new { detail = $"No report data found for event '{eventId}'." })
                : Results.Ok(report);
        })
        .WithName("GetEventSalesReport")
        .WithSummary("Get ticket sales summary for a specific event");

        return app;
    }
}
