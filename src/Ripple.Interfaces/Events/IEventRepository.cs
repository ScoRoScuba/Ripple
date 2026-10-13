using Ripple.Domain.Entities;

namespace Ripple.Events;

public interface IEventRepository
{
    Task<Event?> GetByIdWithTiersAsync(Guid id);
    Task<IEnumerable<Event>> GetAllWithTiersAsync();
    Task<Event> CreateAsync(Event @event, IEnumerable<PricingTier> tiers);
    Task UpdateAsync(Event @event, IEnumerable<PricingTier> tiers);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
