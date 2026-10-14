using Npgsql;
using Ripple.Migrations;
using Testcontainers.PostgreSql;

namespace Ripple.MigrationTests;

public class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ticketing")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public void Migrations_Apply_Successfully()
    {
        var result = MigrationRunner.Run(_postgres.GetConnectionString());

        Assert.True(result.Successful, result.Error?.Message);
    }

    [Fact]
    public void Migrations_Apply_When_Database_Does_Not_Exist()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = "fresh_db"
        }.ConnectionString;

        // Same sequence as Program.cs: ensure database, check, then upgrade.
        MigrationRunner.EnsureDatabaseExists(connectionString);

        Assert.True(MigrationRunner.IsUpgradeRequired(connectionString));

        var result = MigrationRunner.Run(connectionString);

        Assert.True(result.Successful, result.Error?.Message);
    }

    [Fact]
    public async Task All_Expected_Tables_Exist_After_Migration()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        var tables = await GetPublicTablesAsync();

        Assert.Contains("events", tables);
        Assert.Contains("pricing_tiers", tables);
        Assert.Contains("tickets", tables);
        Assert.Contains("schemaversions", tables);
    }

    [Fact]
    public void Migrations_Are_Idempotent()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        var secondRun = MigrationRunner.Run(_postgres.GetConnectionString());

        Assert.True(secondRun.Successful, secondRun.Error?.Message);
        Assert.Empty(secondRun.Scripts);
    }

    [Fact]
    public async Task Applied_Script_Count_Matches_Scripts_On_Disk()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM schemaversions", conn);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        var scriptsOnDisk = typeof(MigrationRunner).Assembly
            .GetManifestResourceNames()
            .Count(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

        Assert.True(scriptsOnDisk > 0, "No embedded migration scripts found.");
        Assert.Equal(scriptsOnDisk, count);
    }

    [Fact]
    public async Task Events_Table_Enforces_Positive_Capacity()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO events (name, venue, date, time, total_capacity) VALUES ('X', 'Y', '2025-01-01', '19:00', -1)",
                conn);
            await cmd.ExecuteNonQueryAsync();
        });
    }

    [Fact]
    public async Task PricingTiers_Table_Enforces_TicketsSold_Le_Capacity()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        var eventId = await InsertEventAsync(conn);

        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO pricing_tiers (event_id, name, price, capacity, tickets_sold)
                  VALUES (@EventId, 'GA', 10.00, 100, 200)",
                conn);
            cmd.Parameters.AddWithValue("EventId", eventId);
            await cmd.ExecuteNonQueryAsync();
        });
    }

    [Fact]
    public async Task Tickets_Table_Enforces_FK_To_PricingTier()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        var eventId = await InsertEventAsync(conn);

        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO tickets (event_id, pricing_tier_id, purchaser_name, purchaser_email, quantity, unit_price)
                  VALUES (@EventId, @TierId, 'Alice', 'alice@example.com', 1, 10.00)",
                conn);
            cmd.Parameters.AddWithValue("EventId", eventId);
            cmd.Parameters.AddWithValue("TierId", Guid.NewGuid()); // non-existent tier
            await cmd.ExecuteNonQueryAsync();
        });
    }

    [Fact]
    public async Task Indexes_Exist_On_Expected_Columns()
    {
        MigrationRunner.Run(_postgres.GetConnectionString());

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT indexname FROM pg_indexes
              WHERE schemaname = 'public'
              AND indexname IN ('idx_tickets_event_id', 'idx_pricing_tiers_event_id')",
            conn);

        var indexes = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            indexes.Add(reader.GetString(0));

        Assert.Contains("idx_tickets_event_id", indexes);
        Assert.Contains("idx_pricing_tiers_event_id", indexes);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<List<string>> GetPublicTablesAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
            conn);

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        return tables;
    }

    private static async Task<Guid> InsertEventAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO events (name, venue, date, time, total_capacity)
              VALUES ('Test Event', 'Test Venue', '2025-06-01', '19:00', 500)
              RETURNING id",
            conn);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }
}
