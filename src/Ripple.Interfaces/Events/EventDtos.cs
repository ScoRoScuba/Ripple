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
    List<CreatePricingTierRequest> PricingTiers);

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
