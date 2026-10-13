namespace Ripple.Tickets;

public record PurchaseTicketRequest(
    Guid PricingTierId,
    int Quantity,
    string PurchaserName,
    string PurchaserEmail);

public record TicketPurchaseResponse(
    Guid TicketId,
    Guid EventId,
    Guid PricingTierId,
    string PricingTierName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string PurchaserName,
    string PurchaserEmail,
    DateTime PurchasedAt);

public record EventAvailabilityResponse(
    Guid EventId,
    string EventName,
    int TotalCapacity,
    int TotalTicketsSold,
    int TotalAvailable,
    List<TierAvailabilityResponse> Tiers);

public record TierAvailabilityResponse(
    Guid TierId,
    string TierName,
    decimal Price,
    int Capacity,
    int TicketsSold,
    int Available);
