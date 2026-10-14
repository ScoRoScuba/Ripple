using Dapper;
using Ripple.Data;
using Ripple.Reports;

namespace Ripple.DataStore.Repositories;

public class ReportRepository(IDbConnectionFactory connectionFactory) : IReportRepository
{
    // Sales come from the ticket purchase records, not the tiers' running tickets_sold counter:
    // revenue is what buyers actually paid (quantity x unit_price at purchase), so later price
    // changes never rewrite past revenue.
    private const string SelectSql =
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
            COALESCE(SUM(t.quantity), 0)::int                  AS tickets_sold,
            COALESCE(SUM(t.quantity * t.unit_price), 0)::numeric AS revenue
          FROM events e
          JOIN pricing_tiers pt ON pt.event_id = e.id
          LEFT JOIN tickets t ON t.pricing_tier_id = pt.id";

    private const string GroupBySql = " GROUP BY e.id, pt.id";

    public async Task<IEnumerable<TierSalesSummary>> GetEventSalesAsync(Guid eventId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<TierSalesSummary>(
            $"{SelectSql} WHERE e.id = @EventId{GroupBySql} ORDER BY pt.name",
            new { EventId = eventId });
    }

    public async Task<IEnumerable<TierSalesSummary>> GetAllEventsSalesAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<TierSalesSummary>(
            $"{SelectSql}{GroupBySql} ORDER BY e.date, e.time, pt.name");
    }
}
