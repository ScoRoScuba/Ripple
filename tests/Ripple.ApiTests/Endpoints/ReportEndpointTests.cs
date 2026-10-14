using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Api.Endpoints;
using Ripple.ApiTests.Scaffolding;
using Ripple.Reports;

namespace Ripple.ApiTests.Endpoints;

public class ReportEndpointTests : IClassFixture<CustomWebApplicationFactory<ScenarioStartup<IEndpoint>>>
{
    private readonly HttpClient _client;
    private readonly Mock<IReportService> _reportService;

    public ReportEndpointTests(CustomWebApplicationFactory<ScenarioStartup<IEndpoint>> factory)
    {
        _client = factory.CreateClient();
        _reportService = factory.Services.ResetMock<IReportService>();
    }

    [Fact]
    public async Task GetAllSalesReports_Returns200WithList()
    {
        _reportService.Setup(s => s.GetAllEventsReportAsync())
            .ReturnsAsync([SampleReport(), SampleReport()]);

        var response = await _client.GetAsync("/api/v1/reports/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<EventSalesReport>>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventSalesReport_WhenFound_Returns200()
    {
        var eventId = Guid.NewGuid();
        _reportService.Setup(s => s.GetEventReportAsync(eventId))
            .ReturnsAsync(SampleReport(eventId));

        var response = await _client.GetAsync($"/api/v1/reports/sales/{eventId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEventSalesReport_WhenNoData_Returns404()
    {
        var eventId = Guid.NewGuid();
        _reportService.Setup(s => s.GetEventReportAsync(eventId))
            .ReturnsAsync((EventSalesReport?)null);

        var response = await _client.GetAsync($"/api/v1/reports/sales/{eventId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private static EventSalesReport SampleReport(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Rock Night", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        new TimeOnly(20, 0), 200, 50, 2500m,
        [new(Guid.NewGuid(), "General", 50m, 200, 50, 2500m)]);
}
