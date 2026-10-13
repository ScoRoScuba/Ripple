using Ripple.Domain.Entities;

namespace Ripple.Tickets;

public interface ITicketRepository
{
    Task<Ticket> PurchaseAsync(
        Guid eventId,
        Guid tierId,
        int quantity,
        string purchaserName,
        string purchaserEmail,
        decimal unitPrice);
}
