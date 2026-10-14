using Ripple.Reports;

namespace Ripple.Reports;

public class ReportService(IReportRepository repository) : IReportService
{
    public async Task<EventSalesReport?> GetEventReportAsync(Guid eventId)
    {
        var summaries = (await repository.GetEventSalesAsync(eventId)).ToList();
        return summaries.Count == 0 ? null : BuildReport(summaries);
    }

    public async Task<IEnumerable<EventSalesReport>> GetAllEventsReportAsync()
    {
        var summaries = await repository.GetAllEventsSalesAsync();
        return summaries
            .GroupBy(s => s.EventId)
            .Select(g => BuildReport(g.ToList()));
    }

    private static EventSalesReport BuildReport(List<TierSalesSummary> summaries)
    {
        var first = summaries[0];
        var tierBreakdown = summaries.Select(s => new TierSalesReport(
            s.TierId,
            s.TierName,
            s.TierPrice,
            s.TierCapacity,
            s.TicketsSold,
            s.Revenue)).ToList();

        return new EventSalesReport(
            first.EventId,
            first.EventName,
            first.Venue,
            first.EventDate,
            first.EventTime,
            first.TotalCapacity,
            tierBreakdown.Sum(t => t.TicketsSold),
            tierBreakdown.Sum(t => t.Revenue),
            tierBreakdown);
    }
}
