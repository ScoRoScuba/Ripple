using System.Data;
using Dapper;
using Ripple.Data;
using Ripple.Tickets;
using Ripple.Domain.Entities;
using Ripple.Domain.Exceptions;

namespace Ripple.DataStore.Repositories;

public class TicketRepository(IDbConnectionFactory connectionFactory) : ITicketRepository
{
    public async Task<Ticket> PurchaseAsync(
        Guid eventId,
        Guid tierId,
        int quantity,
        string purchaserName,
        string purchaserEmail,
        decimal unitPrice)
    {
        using var conn = connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        // Lock the tier row to prevent concurrent overselling
        var tier = await conn.QueryFirstOrDefaultAsync<PricingTier>(
            "SELECT id, capacity, tickets_sold FROM pricing_tiers WHERE id = @TierId FOR UPDATE",
            new { TierId = tierId },
            tx);

        if (tier is null) throw new PricingTierNotFoundException(tierId);

        if (tier.TicketsSold + quantity > tier.Capacity)
            throw new InsufficientCapacityException(tier.Available, quantity);

        await conn.ExecuteAsync(
            "UPDATE pricing_tiers SET tickets_sold = tickets_sold + @Quantity WHERE id = @TierId",
            new { Quantity = quantity, TierId = tierId },
            tx);

        var ticket = await conn.QueryFirstAsync<Ticket>(
            @"INSERT INTO tickets (id, event_id, pricing_tier_id, purchaser_name, purchaser_email, quantity, unit_price, purchased_at)
              VALUES (@Id, @EventId, @PricingTierId, @PurchaserName, @PurchaserEmail, @Quantity, @UnitPrice, @PurchasedAt)
              RETURNING id, event_id, pricing_tier_id, purchaser_name, purchaser_email, quantity, unit_price, purchased_at",
            new
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                PricingTierId = tierId,
                PurchaserName = purchaserName,
                PurchaserEmail = purchaserEmail,
                Quantity = quantity,
                UnitPrice = unitPrice,
                PurchasedAt = DateTime.UtcNow
            },
            tx);

        tx.Commit();
        return ticket;
    }
}
