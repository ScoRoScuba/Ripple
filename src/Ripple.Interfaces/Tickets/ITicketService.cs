namespace Ripple.Tickets;

public interface ITicketService
{
    Task<TicketPurchaseResponse> PurchaseAsync(Guid eventId, PurchaseTicketRequest request);
    Task<EventAvailabilityResponse> GetAvailabilityAsync(Guid eventId);
}
