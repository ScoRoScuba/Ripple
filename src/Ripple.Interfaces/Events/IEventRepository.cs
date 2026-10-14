using Ripple.Domain.Entities;

namespace Ripple.Events;

public interface IEventRepository
{
    Task<Event?> GetByIdWithTiersAsync(Guid id);
    Task<IEnumerable<Event>> GetAllWithTiersAsync();
    Task<Event> CreateAsync(Event @event, IEnumerable<PricingTier> tiers);
    /// <summary>
    /// Saves the event and makes <paramref name="tiers"/> its complete set of tiers: existing tiers are
    /// updated in place by id, new ones inserted, and absent ones deleted. Never writes tickets_sold.
    /// Throws <see cref="Domain.Exceptions.SalesHistoryConflictException"/> if the change would remove
    /// a tier with tickets or reduce capacity below tickets sold.
    /// </summary>
    Task UpdateAsync(Event @event, IEnumerable<PricingTier> tiers);

    /// <summary>
    /// Throws <see cref="Domain.Exceptions.SalesHistoryConflictException"/> if the event has tickets.
    /// </summary>
    Task DeleteAsync(Guid id);
}
