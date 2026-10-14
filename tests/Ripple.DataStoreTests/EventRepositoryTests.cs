using FluentAssertions;
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
    public async Task UpdateAsync_Updates_Event_And_Replaces_Its_Tiers()
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
}
