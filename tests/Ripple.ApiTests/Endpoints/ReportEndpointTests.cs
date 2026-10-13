using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Reports;

namespace Ripple.ApiTests.Endpoints;

public class ReportEndpointTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllSalesReports_Returns200WithList()
    {
        factory.MockReportService.Setup(s => s.GetAllEventsReportAsync())
            .ReturnsAsync([SampleReport(), SampleReport()]);

        var response = await _client.GetAsync("/api/reports/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<EventSalesReport>>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventSalesReport_WhenFound_Returns200()
    {
        var eventId = Guid.NewGuid();
        factory.MockReportService.Setup(s => s.GetEventReportAsync(eventId))
            .ReturnsAsync(SampleReport(eventId));

        var response = await _client.GetAsync($"/api/reports/sales/{eventId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEventSalesReport_WhenNoData_Returns404()
    {
        var eventId = Guid.NewGuid();
        factory.MockReportService.Setup(s => s.GetEventReportAsync(eventId))
            .ReturnsAsync((EventSalesReport?)null);

        var response = await _client.GetAsync($"/api/reports/sales/{eventId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static EventSalesReport SampleReport(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Rock Night", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        new TimeOnly(20, 0), 200, 50, 2500m,
        [new(Guid.NewGuid(), "General", 50m, 200, 50, 2500m)]);
}
