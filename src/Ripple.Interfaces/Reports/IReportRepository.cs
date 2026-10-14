namespace Ripple.Reports;

public interface IReportRepository
{
    Task<IEnumerable<TierSalesSummary>> GetEventSalesAsync(Guid eventId);
    Task<IEnumerable<TierSalesSummary>> GetAllEventsSalesAsync();
}

public record TierSalesSummary(
    Guid EventId,
    string EventName,
    string Venue,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    Guid TierId,
    string TierName,
    decimal TierPrice,
    int TierCapacity,
    int TicketsSold,
    decimal Revenue);
