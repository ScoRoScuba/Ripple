using Ripple.Reports;

namespace Ripple.Api.Endpoints.Reports;

public class GetAllSalesReportsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/sales", Handle)
            .WithTags("Reports")
            .WithName("GetAllSalesReports")
            .WithSummary("Get ticket sales summary for all events");

    private static async Task<IResult> Handle(IReportService service) =>
        Results.Ok(await service.GetAllEventsReportAsync());
}
