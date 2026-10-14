using Ripple.Reports;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Reports;

public class GetEventSalesReportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("reports/sales/{eventId:guid}", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Reports")
            .WithName("GetEventSalesReport")
            .WithSummary("Get ticket sales summary for a specific event");

    private static async Task<IResult> Handle(Guid eventId, IReportService service)
    {
        var report = await service.GetEventReportAsync(eventId);
        return report is null
            ? Results.NotFound(new { detail = $"No report data found for event '{eventId}'." })
            : Results.Ok(report);
    }
}
