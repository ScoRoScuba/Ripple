using FluentAssertions;
using Ripple.Domain.Exceptions;
using Ripple.DataStore.Repositories;

namespace Ripple.DataStoreTests;

public class TicketRepositoryTests : PostgresTestBase
{
    [Fact]
    public async Task PurchaseAsync_Records_Ticket_And_Increments_TicketsSold()
    {
        var (evt, tiers) = NewEvent(("GA", 25m, 10));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);
        var tier = tiers[0];

        var ticket = await new TicketRepository(ConnectionFactory)
            .PurchaseAsync(evt.Id, tier.Id, 3, "Alice", "alice@example.com", tier.Price);

        ticket.Quantity.Should().Be(3);
        ticket.PricingTierId.Should().Be(tier.Id);
        (await TicketsSoldAsync(evt.Id)).Should().Be(3);
    }

    [Fact]
    public async Task PurchaseAsync_Over_Capacity_Throws_And_Leaves_TicketsSold_Unchanged()
    {
        var (evt, tiers) = NewEvent(("GA", 25m, 2));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);
        var tier = tiers[0];

        var purchase = () => new TicketRepository(ConnectionFactory)
            .PurchaseAsync(evt.Id, tier.Id, 3, "Alice", "alice@example.com", tier.Price);

        await purchase.Should().ThrowAsync<InsufficientCapacityException>();
        (await TicketsSoldAsync(evt.Id)).Should().Be(0);
    }

    [Fact]
    public async Task PurchaseAsync_For_Unknown_Tier_Throws_PricingTierNotFound()
    {
        var (evt, tiers) = NewEvent(("GA", 25m, 10));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);

        var purchase = () => new TicketRepository(ConnectionFactory)
            .PurchaseAsync(evt.Id, Guid.NewGuid(), 1, "Alice", "alice@example.com", 25m);

        await purchase.Should().ThrowAsync<PricingTierNotFoundException>();
    }

    private async Task<int> TicketsSoldAsync(Guid eventId)
    {
        var stored = await new EventRepository(ConnectionFactory).GetByIdWithTiersAsync(eventId);
        return stored!.PricingTiers.Sum(t => t.TicketsSold);
    }
}
