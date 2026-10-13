namespace Ripple.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid PricingTierId { get; set; }
    public string PurchaserName { get; set; } = string.Empty;
    public string PurchaserEmail { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime PurchasedAt { get; set; }
}
