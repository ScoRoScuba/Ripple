using Dapper;
using Ripple.Data;
using Ripple.Reports;

namespace Ripple.DataStore.Repositories;

public class ReportRepository(IDbConnectionFactory connectionFactory) : IReportRepository
{
    private const string BaseSql =
        @"SELECT
            e.id          AS event_id,
            e.name        AS event_name,
            e.venue,
            e.date        AS event_date,
            e.time        AS event_time,
            e.total_capacity,
            pt.id         AS tier_id,
            pt.name       AS tier_name,
            pt.price      AS tier_price,
            pt.capacity   AS tier_capacity,
            pt.tickets_sold
          FROM events e
          JOIN pricing_tiers pt ON pt.event_id = e.id";

    public async Task<IEnumerable<TierSalesSummary>> GetEventSalesAsync(Guid eventId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<TierSalesSummary>(
            $"{BaseSql} WHERE e.id = @EventId ORDER BY pt.name",
            new { EventId = eventId });
    }

    public async Task<IEnumerable<TierSalesSummary>> GetAllEventsSalesAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<TierSalesSummary>(
            $"{BaseSql} ORDER BY e.date, e.time, pt.name");
    }
}
