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
        var tierCapacitySum = request.PricingTiers.Sum(t => t.Capacity);
        if (tierCapacitySum != request.TotalCapacity)
            throw new ArgumentException(
                $"Sum of tier capacities ({tierCapacitySum}) must equal total capacity ({request.TotalCapacity}).");

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

        var tierCapacitySum = request.PricingTiers.Sum(t => t.Capacity);
        if (tierCapacitySum != request.TotalCapacity)
            throw new ArgumentException(
                $"Sum of tier capacities ({tierCapacitySum}) must equal total capacity ({request.TotalCapacity}).");

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Venue = request.Venue;
        existing.Date = request.Date;
        existing.Time = request.Time;
        existing.TotalCapacity = request.TotalCapacity;
        existing.UpdatedAt = DateTime.UtcNow;

        var tiers = request.PricingTiers.Select(t => new PricingTier
        {
            Id = Guid.NewGuid(),
            EventId = id,
            Name = t.Name,
            Price = t.Price,
            Capacity = t.Capacity,
            TicketsSold = 0
        }).ToList();

        await repository.UpdateAsync(existing, tiers);
        existing.PricingTiers = tiers;

        logger.LogInformation("[EventService]:[UpdateAsync] - Updated event {EventId}; pricing tiers replaced ({TierCount})",
            id, tiers.Count);

        return MapToResponse(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        if (!await repository.ExistsAsync(id)) throw new EventNotFoundException(id);
        await repository.DeleteAsync(id);

        logger.LogInformation("[EventService]:[DeleteAsync] - Deleted event {EventId}", id);
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
