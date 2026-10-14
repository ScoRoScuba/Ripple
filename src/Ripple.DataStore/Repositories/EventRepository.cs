using Dapper;
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

        await conn.ExecuteAsync("DELETE FROM pricing_tiers WHERE event_id = @Id", new { @event.Id }, tx);

        var tierList = tiers.ToList();
        if (tierList.Count > 0)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO pricing_tiers (id, event_id, name, price, capacity, tickets_sold)
                  VALUES (@Id, @EventId, @Name, @Price, @Capacity, @TicketsSold)",
                tierList, tx);
        }

        tx.Commit();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM events WHERE id = @Id", new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM events WHERE id = @Id)", new { Id = id });
    }
}
