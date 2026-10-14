using Dapper;
using DbUp.Engine.Output;
using Ripple.DataStore.Repositories;
using Ripple.Data;
using Ripple.Domain.Entities;
using Ripple.DataStore.Data;
using Ripple.Migrations;
using Testcontainers.PostgreSql;

namespace Ripple.DataStoreTests;

/// <summary>
/// Gives every test its own PostgreSQL container with the real schema applied,
/// so tests never share state.
/// </summary>
public abstract class PostgresTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    protected IDbConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var result = MigrationRunner.Run(_postgres.GetConnectionString(), new NoOpUpgradeLog());
        if (!result.Successful)
            throw new InvalidOperationException("Test database migration failed.", result.Error);

        ConnectionFactory = new DbConnectionFactory(_postgres.GetConnectionString());
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    /// <summary>The tiers' running tickets_sold counter for the event.</summary>
    protected async Task<int> TicketsSoldAsync(Guid eventId)
    {
        var stored = await new EventRepository(ConnectionFactory).GetByIdWithTiersAsync(eventId);
        return stored!.PricingTiers.Sum(t => t.TicketsSold);
    }

    /// <summary>The ticket purchase records for the event: (purchases, tickets).</summary>
    protected async Task<(int Purchases, int Tickets)> TicketRecordsAsync(Guid eventId)
    {
        using var conn = ConnectionFactory.CreateConnection();
        return await conn.QuerySingleAsync<(int, int)>(
            "SELECT COUNT(*)::int, COALESCE(SUM(quantity), 0)::int FROM tickets WHERE event_id = @EventId",
            new { EventId = eventId });
    }

    protected static (Event Event, List<PricingTier> Tiers) NewEvent(params (string Name, decimal Price, int Capacity)[] tiers)
    {
        var eventId = Guid.NewGuid();
        var pricingTiers = tiers.Select(t => new PricingTier
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = t.Name,
            Price = t.Price,
            Capacity = t.Capacity,
            TicketsSold = 0
        }).ToList();

        var @event = new Event
        {
            Id = eventId,
            Name = "Test Event",
            Description = "Integration test event",
            Venue = "Test Venue",
            Date = new DateOnly(2030, 6, 1),
            Time = new TimeOnly(19, 0),
            TotalCapacity = pricingTiers.Sum(t => t.Capacity),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return (@event, pricingTiers);
    }
}
