using FluentAssertions;
using Moq;
using Ripple.Reports;

namespace Ripple.UnitTests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _repoMock = new();
    private readonly ReportService _sut;

    public ReportServiceTests() => _sut = new ReportService(_repoMock.Object);

    [Fact]
    public async Task GetEventReportAsync_WhenNoData_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetEventSalesAsync(It.IsAny<Guid>())).ReturnsAsync([]);

        var result = await _sut.GetEventReportAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEventReportAsync_AggregatesRevenueAndSalesCorrectly()
    {
        var eventId = Guid.NewGuid();
        var summaries = SampleSummaries(eventId);
        _repoMock.Setup(r => r.GetEventSalesAsync(eventId)).ReturnsAsync(summaries);

        var result = await _sut.GetEventReportAsync(eventId);

        // Revenue is what was actually paid, not today's price x tickets sold:
        // General 20 x 50 = 1000; VIP was repriced from 100 to 120 after 5 sales, so 5x100 + 5x120 = 1100.
        result.Should().NotBeNull();
        result!.TotalTicketsSold.Should().Be(30);
        result.TotalRevenue.Should().Be(2100m);
        result.TierBreakdown.Should().HaveCount(2);
        result.TierBreakdown.Single(t => t.TierName == "General").Revenue.Should().Be(1000m);
        result.TierBreakdown.Single(t => t.TierName == "VIP").Revenue.Should().Be(1100m);
    }

    [Fact]
    public async Task GetAllEventsReportAsync_GroupsByEvent()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var summaries = SampleSummaries(eventId1).Concat(SampleSummaries(eventId2));
        _repoMock.Setup(r => r.GetAllEventsSalesAsync()).ReturnsAsync(summaries);

        var results = (await _sut.GetAllEventsReportAsync()).ToList();

        results.Should().HaveCount(2);
        results.Select(r => r.EventId).Should().BeEquivalentTo([eventId1, eventId2]);
        results.Should().OnlyContain(r => r.TotalTicketsSold == 30 && r.TotalRevenue == 2100m);
    }

    private static List<TierSalesSummary> SampleSummaries(Guid eventId) =>
    [
        new(eventId, "Rock Night", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            Guid.NewGuid(), "General", 50m, 150, 20, Revenue: 1000m),
        new(eventId, "Rock Night", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            Guid.NewGuid(), "VIP", 120m, 50, 10, Revenue: 1100m)
    ];
}
