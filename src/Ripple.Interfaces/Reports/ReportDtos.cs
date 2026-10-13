namespace Ripple.Reports;

public record EventSalesReport(
    Guid EventId,
    string EventName,
    string Venue,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    int TotalTicketsSold,
    decimal TotalRevenue,
    List<TierSalesReport> TierBreakdown);

public record TierSalesReport(
    Guid TierId,
    string TierName,
    decimal Price,
    int Capacity,
    int TicketsSold,
    decimal Revenue);
