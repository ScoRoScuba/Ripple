using FluentAssertions;
using Ripple.Domain.Entities;
using Ripple.DataStore.Repositories;

namespace Ripple.DataStoreTests;

public class ReportRepositoryTests : PostgresTestBase
{
    [Fact]
    public async Task GetEventSalesAsync_Reports_Each_Tier_With_Its_Real_Sales()
    {
        var (evt, tiers) = await CreateEventAsync(("GA", 25m, 100), ("VIP", 80m, 20));
        var (ga, vip) = (tiers[0], tiers[1]);
        await PurchaseAsync(evt, ga, 3);
        await PurchaseAsync(evt, ga, 2);
        await PurchaseAsync(evt, vip, 1);

        var rows = (await new ReportRepository(ConnectionFactory).GetEventSalesAsync(evt.Id)).ToList();

        rows.Should().HaveCount(2).And.OnlyContain(r =>
            r.EventId == evt.Id && r.EventName == evt.Name && r.Venue == evt.Venue &&
            r.EventDate == evt.Date && r.EventTime == evt.Time && r.TotalCapacity == evt.TotalCapacity);

        var gaRow = rows.Single(r => r.TierId == ga.Id);
        gaRow.Should().BeEquivalentTo(new { TierName = "GA", TierPrice = 25m, TierCapacity = 100, TicketsSold = 5, Revenue = 125m });

        var vipRow = rows.Single(r => r.TierId == vip.Id);
        vipRow.Should().BeEquivalentTo(new { TierName = "VIP", TierPrice = 80m, TierCapacity = 20, TicketsSold = 1, Revenue = 80m });
    }

    [Fact]
    public async Task GetEventSalesAsync_A_Price_Change_Does_Not_Rewrite_Past_Revenue()
    {
        var (evt, tiers) = await CreateEventAsync(("GA", 25m, 100));
        await PurchaseAsync(evt, tiers[0], 2);                    // 2 x 25 = 50

        var repriced = new PricingTier
        {
            Id = tiers[0].Id, EventId = evt.Id, Name = "GA", Price = 40m, Capacity = 100
        };
        await new EventRepository(ConnectionFactory).UpdateAsync(evt, [repriced]);
        await PurchaseAsync(evt, repriced, 1);                    // 1 x 40 = 40

        var row = (await new ReportRepository(ConnectionFactory).GetEventSalesAsync(evt.Id)).Single();

        row.TierPrice.Should().Be(40m, "the tier shows its current price");
        row.TicketsSold.Should().Be(3);
        row.Revenue.Should().Be(90m, "revenue is what buyers paid, not 3 x today's price (120)");
    }

    [Fact]
    public async Task GetEventSalesAsync_Includes_Tiers_With_No_Sales()
    {
        var (evt, _) = await CreateEventAsync(("GA", 25m, 100), ("VIP", 80m, 20));

        var rows = (await new ReportRepository(ConnectionFactory).GetEventSalesAsync(evt.Id)).ToList();

        rows.Should().HaveCount(2).And.OnlyContain(r => r.TicketsSold == 0 && r.Revenue == 0m);
    }

    [Fact]
    public async Task GetEventSalesAsync_For_Unknown_Event_Returns_Nothing()
    {
        await CreateEventAsync(("GA", 25m, 100));

        var rows = await new ReportRepository(ConnectionFactory).GetEventSalesAsync(Guid.NewGuid());

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllEventsSalesAsync_Reports_Every_Event_Without_Mixing_Their_Sales()
    {
        var (soldEvent, soldTiers) = await CreateEventAsync(("GA", 25m, 100));
        var (unsoldEvent, _) = await CreateEventAsync(("GA", 40m, 50), ("VIP", 90m, 10));
        await PurchaseAsync(soldEvent, soldTiers[0], 4);

        var rows = (await new ReportRepository(ConnectionFactory).GetAllEventsSalesAsync()).ToList();

        rows.Should().HaveCount(3);
        rows.Where(r => r.EventId == soldEvent.Id).Should().ContainSingle()
            .Which.TicketsSold.Should().Be(4);
        rows.Where(r => r.EventId == unsoldEvent.Id).Should().HaveCount(2)
            .And.OnlyContain(r => r.TicketsSold == 0);
    }

    private async Task<(Event Event, List<PricingTier> Tiers)> CreateEventAsync(
        params (string Name, decimal Price, int Capacity)[] tiers)
    {
        var (evt, pricingTiers) = NewEvent(tiers);
        await new EventRepository(ConnectionFactory).CreateAsync(evt, pricingTiers);
        return (evt, pricingTiers);
    }

    private Task PurchaseAsync(Event evt, PricingTier tier, int quantity) =>
        new TicketRepository(ConnectionFactory)
            .PurchaseAsync(evt.Id, tier.Id, quantity, "Buyer", "buyer@example.com", tier.Price);
}
