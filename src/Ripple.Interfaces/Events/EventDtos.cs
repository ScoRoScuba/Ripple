namespace Ripple.Events;

public record CreateEventRequest(
    string Name,
    string Description,
    string Venue,
    DateOnly Date,
    TimeOnly Time,
    int TotalCapacity,
    List<CreatePricingTierRequest> PricingTiers);

public record CreatePricingTierRequest(
    string Name,
    decimal Price,
    int Capacity);

public record UpdateEventRequest(
    string Name,
    string Description,
    string Venue,
    DateOnly Date,
    TimeOnly Time,
    int TotalCapacity,
    List<UpdatePricingTierRequest> PricingTiers);

/// <summary>
/// A pricing tier in an event update. With an <see cref="Id"/>, the existing tier is updated in place
/// (its sales are kept); without one, a new tier is added. Existing tiers left out are removed,
/// which is refused if they have sales.
/// </summary>
public record UpdatePricingTierRequest(
    Guid? Id,
    string Name,
    decimal Price,
    int Capacity);

public record EventResponse(
    Guid Id,
    string Name,
    string Description,
    string Venue,
    DateOnly Date,
    TimeOnly Time,
    int TotalCapacity,
    List<PricingTierResponse> PricingTiers,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PricingTierResponse(
    Guid Id,
    string Name,
    decimal Price,
    int Capacity,
    int TicketsSold,
    int Available);
