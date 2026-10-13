namespace Ripple.Events;

public interface IEventService
{
    Task<EventResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<EventResponse>> GetAllAsync();
    Task<EventResponse> CreateAsync(CreateEventRequest request);
    Task<EventResponse> UpdateAsync(Guid id, UpdateEventRequest request);
    Task DeleteAsync(Guid id);
}
