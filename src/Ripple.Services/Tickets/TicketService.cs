using Ripple.Events;
using Ripple.Tickets;
using Ripple.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ripple.Tickets;

public class TicketService(
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    ILogger<TicketService> logger) : ITicketService
{
    public async Task<TicketPurchaseResponse> PurchaseAsync(Guid eventId, PurchaseTicketRequest request)
    {
        var evt = await eventRepository.GetByIdWithTiersAsync(eventId);
        if (evt is null) throw new EventNotFoundException(eventId);

        var tier = evt.PricingTiers.FirstOrDefault(t => t.Id == request.PricingTierId);
        if (tier is null) throw new PricingTierNotFoundException(request.PricingTierId);

        var ticket = await ticketRepository.PurchaseAsync(
            eventId,
            request.PricingTierId,
            request.Quantity,
            request.PurchaserName,
            request.PurchaserEmail,
            tier.Price);

        logger.LogInformation(
            "[TicketService]:[PurchaseAsync] - Ticket {TicketId}: {Quantity} x '{TierName}' for event {EventId} (tier {PricingTierId})",
            ticket.Id, ticket.Quantity, tier.Name, eventId, tier.Id);

        return new TicketPurchaseResponse(
            ticket.Id,
            ticket.EventId,
            ticket.PricingTierId,
            tier.Name,
            ticket.UnitPrice,
            ticket.Quantity,
            ticket.UnitPrice * ticket.Quantity,
            ticket.PurchaserName,
            ticket.PurchaserEmail,
            ticket.PurchasedAt);
    }

    public async Task<EventAvailabilityResponse> GetAvailabilityAsync(Guid eventId)
    {
        var evt = await eventRepository.GetByIdWithTiersAsync(eventId);
        if (evt is null) throw new EventNotFoundException(eventId);

        return new EventAvailabilityResponse(
            evt.Id,
            evt.Name,
            evt.TotalCapacity,
            evt.PricingTiers.Sum(t => t.TicketsSold),
            evt.PricingTiers.Sum(t => t.Available),
            evt.PricingTiers.Select(t =>
                new TierAvailabilityResponse(t.Id, t.Name, t.Price, t.Capacity, t.TicketsSold, t.Available)).ToList());
    }
}
