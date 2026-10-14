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
    public async Task PurchaseAsync_Buying_Exactly_The_Remaining_Tickets_Succeeds_Then_Tier_Is_Sold_Out()
    {
        var (evt, tiers) = NewEvent(("GA", 25m, 5));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);
        var tier = tiers[0];
        var repository = new TicketRepository(ConnectionFactory);

        await repository.PurchaseAsync(evt.Id, tier.Id, 3, "Alice", "alice@example.com", tier.Price);
        var lastTickets = await repository.PurchaseAsync(evt.Id, tier.Id, 2, "Bob", "bob@example.com", tier.Price);

        lastTickets.Quantity.Should().Be(2);
        (await TicketsSoldAsync(evt.Id)).Should().Be(5);

        var oneMore = () => repository.PurchaseAsync(evt.Id, tier.Id, 1, "Carol", "carol@example.com", tier.Price);
        await oneMore.Should().ThrowAsync<InsufficientCapacityException>();
        (await TicketsSoldAsync(evt.Id)).Should().Be(5);
    }

    [Fact]
    public async Task PurchaseAsync_Concurrent_Purchases_Never_Oversell()
    {
        const int capacity = 10;
        const int buyers = 30;

        var (evt, tiers) = NewEvent(("GA", 25m, capacity));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);
        var tier = tiers[0];

        // Release every buyer at once so the purchases genuinely contend for the same tier row.
        using var start = new ManualResetEventSlim(false);
        var purchases = Enumerable.Range(0, buyers).Select(i => Task.Run(async () =>
        {
            start.Wait();
            try
            {
                await new TicketRepository(ConnectionFactory)
                    .PurchaseAsync(evt.Id, tier.Id, 1, $"Buyer {i}", $"buyer{i}@example.com", tier.Price);
                return (Exception?)null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        })).ToList();

        start.Set();
        var failures = (await Task.WhenAll(purchases)).Where(ex => ex is not null).ToList();

        failures.Should().HaveCount(buyers - capacity);
        failures.Should().AllBeOfType<InsufficientCapacityException>(
            "running out of tickets must be reported as insufficient capacity, not a database error");
        (await TicketsSoldAsync(evt.Id)).Should().Be(capacity);
        (await TicketRecordsAsync(evt.Id)).Purchases.Should().Be(capacity);
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
}
