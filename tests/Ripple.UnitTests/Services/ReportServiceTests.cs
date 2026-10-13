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

        result.Should().NotBeNull();
        result!.TotalTicketsSold.Should().Be(30);
        result.TotalRevenue.Should().Be(30 * 50m);
        result.TierBreakdown.Should().HaveCount(2);
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
        results.Should().OnlyContain(r => r.TotalTicketsSold == 30);
    }

    private static List<TierSalesSummary> SampleSummaries(Guid eventId) =>
    [
        new(eventId, "Rock Night", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            Guid.NewGuid(), "General", 50m, 150, 20),
        new(eventId, "Rock Night", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            Guid.NewGuid(), "VIP", 50m, 50, 10)
    ];
}
