namespace Ripple.Reports;

public interface IReportService
{
    Task<EventSalesReport?> GetEventReportAsync(Guid eventId);
    Task<IEnumerable<EventSalesReport>> GetAllEventsReportAsync();
}
