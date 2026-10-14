using Dapper;
using Npgsql;
using Ripple.Domain.Exceptions;
using Ripple.Data;
using Ripple.Events;
using Ripple.Domain.Entities;

namespace Ripple.DataStore.Repositories;

public class EventRepository(IDbConnectionFactory connectionFactory) : IEventRepository
{
    public async Task<Event?> GetByIdWithTiersAsync(Guid id)
    {
        using var conn = connectionFactory.CreateConnection();

        var evt = await conn.QueryFirstOrDefaultAsync<Event>(
            "SELECT id, name, description, venue, date, time, total_capacity, created_at, updated_at FROM events WHERE id = @Id",
            new { Id = id });

        if (evt is null) return null;

        evt.PricingTiers = (await conn.QueryAsync<PricingTier>(
            "SELECT id, event_id, name, price, capacity, tickets_sold FROM pricing_tiers WHERE event_id = @EventId ORDER BY name",
            new { EventId = id })).ToList();

        return evt;
    }

    public async Task<IEnumerable<Event>> GetAllWithTiersAsync()
    {
        using var conn = connectionFactory.CreateConnection();

        var events = (await conn.QueryAsync<Event>(
            "SELECT id, name, description, venue, date, time, total_capacity, created_at, updated_at FROM events ORDER BY date, time")).ToList();

        if (events.Count == 0) return events;

        var tiers = await conn.QueryAsync<PricingTier>(
            "SELECT id, event_id, name, price, capacity, tickets_sold FROM pricing_tiers WHERE event_id = ANY(@Ids) ORDER BY name",
            new { Ids = events.Select(e => e.Id).ToArray() });

        var tiersByEvent = tiers.GroupBy(t => t.EventId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var evt in events)
            evt.PricingTiers = tiersByEvent.GetValueOrDefault(evt.Id) ?? [];

        return events;
    }

    public async Task<Event> CreateAsync(Event @event, IEnumerable<PricingTier> tiers)
    {
        using var conn = connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            @"INSERT INTO events (id, name, description, venue, date, time, total_capacity, created_at, updated_at)
              VALUES (@Id, @Name, @Description, @Venue, @Date, @Time, @TotalCapacity, @CreatedAt, @UpdatedAt)",
            @event, tx);

        var tierList = tiers.ToList();
        if (tierList.Count > 0)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO pricing_tiers (id, event_id, name, price, capacity, tickets_sold)
                  VALUES (@Id, @EventId, @Name, @Price, @Capacity, @TicketsSold)",
                tierList, tx);
        }

        tx.Commit();
        @event.PricingTiers = tierList;
        return @event;
    }

    public async Task UpdateAsync(Event @event, IEnumerable<PricingTier> tiers)
    {
        using var conn = connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            @"UPDATE events SET name = @Name, description = @Description, venue = @Venue,
              date = @Date, time = @Time, total_capacity = @TotalCapacity, updated_at = @UpdatedAt
              WHERE id = @Id",
            @event, tx);

        var tierList = tiers.ToList();

        try
        {
            // Remove only the tiers no longer wanted. The tickets foreign key (ON DELETE RESTRICT)
            // refuses this if any of them has sales.
            await conn.ExecuteAsync(
                "DELETE FROM pricing_tiers WHERE event_id = @EventId AND NOT (id = ANY(@KeepIds))",
                new { EventId = @event.Id, KeepIds = tierList.Select(t => t.Id).ToArray() }, tx);

            // Add new tiers and update kept ones in place. tickets_sold is deliberately never written:
            // it is owned by ticket purchases, so sales recorded since the event was read are preserved.
            await conn.ExecuteAsync(
                @"INSERT INTO pricing_tiers (id, event_id, name, price, capacity, tickets_sold)
                  VALUES (@Id, @EventId, @Name, @Price, @Capacity, 0)
                  ON CONFLICT (id) DO UPDATE
                  SET name = EXCLUDED.name, price = EXCLUDED.price, capacity = EXCLUDED.capacity
                  WHERE pricing_tiers.event_id = EXCLUDED.event_id",
                tierList, tx);
        }
        catch (PostgresException ex) when (IsSalesHistoryViolation(ex))
        {
            // The service checks these rules first; this covers tickets sold while the update was in flight.
            throw new SalesHistoryConflictException(
                "The change conflicts with tickets that have been sold for this event. Reload the event and try again.");
        }

        tx.Commit();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = connectionFactory.CreateConnection();

        try
        {
            await conn.ExecuteAsync("DELETE FROM events WHERE id = @Id", new { Id = id });
        }
        catch (PostgresException ex) when (IsSalesHistoryViolation(ex))
        {
            throw new SalesHistoryConflictException($"Event '{id}' has tickets sold and cannot be deleted.");
        }
    }

    // Foreign key violation: deleting a tier/event that tickets reference.
    // Check violation on chk_tickets_le_capacity: capacity reduced below tickets sold.
    private static bool IsSalesHistoryViolation(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.ForeignKeyViolation ||
        (ex.SqlState == PostgresErrorCodes.CheckViolation && ex.ConstraintName == "chk_tickets_le_capacity");
}
