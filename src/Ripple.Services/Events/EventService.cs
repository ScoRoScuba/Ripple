using Ripple.Events;
using Ripple.Domain.Entities;
using Ripple.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ripple.Events;

public class EventService(IEventRepository repository, ILogger<EventService> logger) : IEventService
{
    public async Task<EventResponse> GetByIdAsync(Guid id)
    {
        var evt = await repository.GetByIdWithTiersAsync(id);
        if (evt is null) throw new EventNotFoundException(id);
        return MapToResponse(evt);
    }

    public async Task<IEnumerable<EventResponse>> GetAllAsync()
    {
        var events = await repository.GetAllWithTiersAsync();
        return events.Select(MapToResponse);
    }

    public async Task<EventResponse> CreateAsync(CreateEventRequest request)
    {
        EnsureTierCapacitiesMatchTotal(request.PricingTiers.Sum(t => t.Capacity), request.TotalCapacity);

        var eventId = Guid.NewGuid();
        var @event = new Event
        {
            Id = eventId,
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            Date = request.Date,
            Time = request.Time,
            TotalCapacity = request.TotalCapacity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tiers = request.PricingTiers.Select(t => new PricingTier
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = t.Name,
            Price = t.Price,
            Capacity = t.Capacity,
            TicketsSold = 0
        }).ToList();

        var created = await repository.CreateAsync(@event, tiers);

        logger.LogInformation("[EventService]:[CreateAsync] - Created event {EventId} '{EventName}' with {TierCount} pricing tiers",
            created.Id, created.Name, tiers.Count);

        return MapToResponse(created);
    }

    public async Task<EventResponse> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        var existing = await repository.GetByIdWithTiersAsync(id);
        if (existing is null) throw new EventNotFoundException(id);

        EnsureTierCapacitiesMatchTotal(request.PricingTiers.Sum(t => t.Capacity), request.TotalCapacity);
        var tiers = ApplyTierChanges(existing, request.PricingTiers);

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Venue = request.Venue;
        existing.Date = request.Date;
        existing.Time = request.Time;
        existing.TotalCapacity = request.TotalCapacity;
        existing.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existing, tiers);
        existing.PricingTiers = tiers;

        logger.LogInformation("[EventService]:[UpdateAsync] - Updated event {EventId} with {TierCount} pricing tiers",
            id, tiers.Count);

        return MapToResponse(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await repository.GetByIdWithTiersAsync(id);
        if (existing is null) throw new EventNotFoundException(id);

        var ticketsSold = existing.PricingTiers.Sum(t => t.TicketsSold);
        if (ticketsSold > 0)
            throw new SalesHistoryConflictException(
                $"Event '{existing.Name}' has {ticketsSold} tickets sold and cannot be deleted.");

        await repository.DeleteAsync(id);

        logger.LogInformation("[EventService]:[DeleteAsync] - Deleted event {EventId}", id);
    }

    private static void EnsureTierCapacitiesMatchTotal(int tierCapacitySum, int totalCapacity)
    {
        if (tierCapacitySum != totalCapacity)
            throw new ArgumentException(
                $"Sum of tier capacities ({tierCapacitySum}) must equal total capacity ({totalCapacity}).");
    }

    /// <summary>
    /// Works out the event's new set of tiers without disturbing sales history: requested tiers with an
    /// id update that tier in place (keeping its tickets sold), those without an id are added, and
    /// existing tiers left out are removed — unless they have sales.
    /// </summary>
    private static List<PricingTier> ApplyTierChanges(Event existing, IReadOnlyCollection<UpdatePricingTierRequest> requested)
    {
        var duplicateId = requested.Where(t => t.Id is not null).GroupBy(t => t.Id).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicateId is not null)
            throw new ArgumentException($"Pricing tier '{duplicateId}' appears more than once.");

        var removedWithSales = existing.PricingTiers
            .FirstOrDefault(t => t.TicketsSold > 0 && requested.All(r => r.Id != t.Id));
        if (removedWithSales is not null)
            throw new SalesHistoryConflictException(
                $"Pricing tier '{removedWithSales.Name}' has {removedWithSales.TicketsSold} tickets sold and cannot be removed.");

        var current = existing.PricingTiers.ToDictionary(t => t.Id);

        return requested.Select(change =>
        {
            if (change.Id is null)
                return new PricingTier
                {
                    Id = Guid.NewGuid(), EventId = existing.Id,
                    Name = change.Name, Price = change.Price, Capacity = change.Capacity, TicketsSold = 0
                };

            if (!current.TryGetValue(change.Id.Value, out var tier))
                throw new PricingTierNotFoundException(change.Id.Value);

            if (change.Capacity < tier.TicketsSold)
                throw new SalesHistoryConflictException(
                    $"Pricing tier '{tier.Name}' cannot be reduced to {change.Capacity}: {tier.TicketsSold} tickets are already sold.");

            return new PricingTier
            {
                Id = tier.Id, EventId = existing.Id,
                Name = change.Name, Price = change.Price, Capacity = change.Capacity, TicketsSold = tier.TicketsSold
            };
        }).ToList();
    }

    private static EventResponse MapToResponse(Event evt) => new(
        evt.Id,
        evt.Name,
        evt.Description,
        evt.Venue,
        evt.Date,
        evt.Time,
        evt.TotalCapacity,
        evt.PricingTiers.Select(t => new PricingTierResponse(t.Id, t.Name, t.Price, t.Capacity, t.TicketsSold, t.Available)).ToList(),
        evt.CreatedAt,
        evt.UpdatedAt);
}
