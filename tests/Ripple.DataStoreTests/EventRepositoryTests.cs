using FluentAssertions;
using Ripple.Domain.Exceptions;
using Ripple.Domain.Entities;
using Ripple.DataStore.Repositories;

namespace Ripple.DataStoreTests;

public class EventRepositoryTests : PostgresTestBase
{
    [Fact]
    public async Task CreateAsync_Persists_Event_And_Its_Tiers()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, tiers) = NewEvent(("GA", 25m, 100), ("VIP", 80m, 20));

        await repository.CreateAsync(evt, tiers);

        var stored = await repository.GetByIdWithTiersAsync(evt.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be(evt.Name);
        stored.PricingTiers.Select(t => t.Name).Should().BeEquivalentTo("GA", "VIP");
    }

    [Fact]
    public async Task UpdateAsync_Can_Replace_A_Tier_That_Has_No_Sales()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, tiers) = NewEvent(("GA", 25m, 100));
        await repository.CreateAsync(evt, tiers);

        evt.Name = "Renamed Event";
        var replacement = new List<PricingTier>
        {
            new() { Id = Guid.NewGuid(), EventId = evt.Id, Name = "Early Bird", Price = 15m, Capacity = 100, TicketsSold = 0 }
        };

        await repository.UpdateAsync(evt, replacement);

        var stored = await repository.GetByIdWithTiersAsync(evt.Id);
        stored!.Name.Should().Be("Renamed Event");
        stored.PricingTiers.Should().ContainSingle().Which.Name.Should().Be("Early Bird");
    }

    // ── Sales history ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Editing_An_Event_Keeps_Its_Sales_And_Purchases()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, tiers) = await CreateEventWithSalesAsync(capacity: 10, sold: 8);

        evt.Description = "Doors open at 7pm";
        // TicketsSold = 0 on purpose: the repository must never write the sales counter.
        await repository.UpdateAsync(evt, [WithChanges(tiers[0], price: 60m, ticketsSold: 0)]);

        (await TicketsSoldAsync(evt.Id)).Should().Be(8);
        (await TicketRecordsAsync(evt.Id)).Should().Be((1, 8));
        var stored = await repository.GetByIdWithTiersAsync(evt.Id);
        stored!.Description.Should().Be("Doors open at 7pm");
        stored.PricingTiers.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { tiers[0].Id, Price = 60m, TicketsSold = 8, Available = 2 });
    }

    [Fact]
    public async Task UpdateAsync_Removing_A_Tier_With_Tickets_Is_Refused_And_Nothing_Changes()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, _) = await CreateEventWithSalesAsync(capacity: 10, sold: 3);
        var originalName = evt.Name;

        evt.Name = "Should Not Be Saved";
        var replacement = new PricingTier { Id = Guid.NewGuid(), EventId = evt.Id, Name = "New", Price = 10m, Capacity = 10 };

        var update = () => repository.UpdateAsync(evt, [replacement]);

        await update.Should().ThrowAsync<SalesHistoryConflictException>();
        (await repository.GetByIdWithTiersAsync(evt.Id))!.Name.Should().Be(originalName, "the whole update is rolled back");
        (await TicketRecordsAsync(evt.Id)).Should().Be((1, 3));
        (await TicketsSoldAsync(evt.Id)).Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_Reducing_Capacity_Below_Tickets_Sold_Is_Refused()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, tiers) = await CreateEventWithSalesAsync(capacity: 10, sold: 6);

        var update = () => repository.UpdateAsync(evt, [WithChanges(tiers[0], capacity: 5)]);

        await update.Should().ThrowAsync<SalesHistoryConflictException>();
        (await repository.GetByIdWithTiersAsync(evt.Id))!.PricingTiers.Single().Capacity.Should().Be(10);
    }

    [Fact]
    public async Task DeleteAsync_An_Event_With_Tickets_Is_Refused_And_Its_History_Kept()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, _) = await CreateEventWithSalesAsync(capacity: 10, sold: 2);

        var delete = () => repository.DeleteAsync(evt.Id);

        await delete.Should().ThrowAsync<SalesHistoryConflictException>();
        (await repository.GetByIdWithTiersAsync(evt.Id)).Should().NotBeNull();
        (await TicketRecordsAsync(evt.Id)).Should().Be((1, 2));
    }

    [Fact]
    public async Task DeleteAsync_An_Event_Without_Tickets_Removes_It_And_Its_Tiers()
    {
        var repository = new EventRepository(ConnectionFactory);
        var (evt, tiers) = NewEvent(("GA", 25m, 100), ("VIP", 80m, 20));
        await repository.CreateAsync(evt, tiers);

        await repository.DeleteAsync(evt.Id);

        (await repository.GetByIdWithTiersAsync(evt.Id)).Should().BeNull();
    }

    private async Task<(Event Event, List<PricingTier> Tiers)> CreateEventWithSalesAsync(int capacity, int sold)
    {
        var (evt, tiers) = NewEvent(("GA", 50m, capacity));
        await new EventRepository(ConnectionFactory).CreateAsync(evt, tiers);
        await new TicketRepository(ConnectionFactory)
            .PurchaseAsync(evt.Id, tiers[0].Id, sold, "Alice", "alice@example.com", tiers[0].Price);
        return (evt, tiers);
    }

    private static PricingTier WithChanges(PricingTier tier, decimal? price = null, int? capacity = null, int? ticketsSold = null) => new()
    {
        Id = tier.Id,
        EventId = tier.EventId,
        Name = tier.Name,
        Price = price ?? tier.Price,
        Capacity = capacity ?? tier.Capacity,
        TicketsSold = ticketsSold ?? tier.TicketsSold
    };
}
