using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Moq;
using Ripple.Events;
using Ripple.Domain.Entities;
using Ripple.Domain.Exceptions;

namespace Ripple.UnitTests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repoMock = new();
    private readonly EventService _sut;

    public EventServiceTests() => _sut = new EventService(_repoMock.Object, NullLogger<EventService>.Instance);

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsResponse()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithTiersAsync(id)).ReturnsAsync(SampleEvent(id));

        var result = await _sut.GetByIdAsync(id);

        result.Id.Should().Be(id);
        result.Name.Should().Be("Rock Night");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsEventNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdWithTiersAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllWithTiersAsync()).ReturnsAsync([SampleEvent(id)]);

        var results = (await _sut.GetAllAsync()).ToList();

        results.Should().HaveCount(1);
        results[0].Id.Should().Be(id);
    }

    [Fact]
    public async Task CreateAsync_WhenTierCapacitiesSumToTotal_CreatesEvent()
    {
        var request = new CreateEventRequest(
            "Rock Night", "A great show", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            [new("General", 50m, 150), new("VIP", 120m, 50)]);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Event>(), It.IsAny<IEnumerable<PricingTier>>()))
            .ReturnsAsync((Event e, IEnumerable<PricingTier> tiers) =>
            {
                e.PricingTiers = tiers.ToList();
                return e;
            });

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("Rock Night");
        result.PricingTiers.Should().HaveCount(2);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Event>(), It.Is<IEnumerable<PricingTier>>(t => t.Count() == 2)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTierCapacitiesDontSumToTotal_ThrowsArgumentException()
    {
        var request = new CreateEventRequest(
            "Rock Night", "", "The Arena",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 200,
            [new("General", 50m, 100)]);

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*100*200*");
    }

    [Fact]
    public async Task UpdateAsync_Keeps_Existing_Tiers_And_Their_Sales()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);
        var saved = CaptureSavedTiers();

        var result = await _sut.UpdateAsync(evt.Id, UpdateRequest(200,
            new(general.Id, "General Admission", 55m, 150),
            new(vip.Id, "VIP", 130m, 50)));

        saved().Should().BeEquivalentTo(new[]
        {
            new { general.Id, Name = "General Admission", Price = 55m, Capacity = 150, TicketsSold = 10 },
            new { vip.Id, Name = "VIP", Price = 130m, Capacity = 50, TicketsSold = 5 }
        });
        result.PricingTiers.Sum(t => t.TicketsSold).Should().Be(15);
    }

    [Fact]
    public async Task UpdateAsync_Adds_A_Tier_Without_An_Id_With_No_Sales()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);
        var saved = CaptureSavedTiers();

        await _sut.UpdateAsync(evt.Id, UpdateRequest(220,
            new(general.Id, "General", 50m, 150),
            new(vip.Id, "VIP", 120m, 50),
            new(null, "Early Bird", 30m, 20)));

        var added = saved().Should().HaveCount(3).And.ContainSingle(t => t.Name == "Early Bird").Subject;
        added.Id.Should().NotBeEmpty().And.NotBe(general.Id).And.NotBe(vip.Id);
        added.TicketsSold.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_Removing_A_Tier_Without_Sales_Is_Allowed()
    {
        var evt = GivenEvent(vipSold: 0);
        var general = evt.PricingTiers[0];
        var saved = CaptureSavedTiers();

        await _sut.UpdateAsync(evt.Id, UpdateRequest(150, new UpdatePricingTierRequest(general.Id, "General", 50m, 150)));

        saved().Should().ContainSingle().Which.Id.Should().Be(general.Id);
    }

    [Fact]
    public async Task UpdateAsync_Removing_A_Tier_With_Sales_Throws_And_Saves_Nothing()
    {
        var evt = GivenEvent();
        var general = evt.PricingTiers[0];

        await _sut.Invoking(s => s.UpdateAsync(evt.Id, UpdateRequest(150, new UpdatePricingTierRequest(general.Id, "General", 50m, 150))))
            .Should().ThrowAsync<SalesHistoryConflictException>().WithMessage("*VIP*5 tickets sold*");

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<IEnumerable<PricingTier>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Reducing_Capacity_Below_Tickets_Sold_Throws_And_Saves_Nothing()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);

        await _sut.Invoking(s => s.UpdateAsync(evt.Id, UpdateRequest(154,
                new(general.Id, "General", 50m, 150),
                new(vip.Id, "VIP", 120m, 4))))
            .Should().ThrowAsync<SalesHistoryConflictException>().WithMessage("*VIP*4*5 tickets*");

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<IEnumerable<PricingTier>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Reducing_Capacity_To_Exactly_Tickets_Sold_Is_Allowed()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);
        var saved = CaptureSavedTiers();

        await _sut.UpdateAsync(evt.Id, UpdateRequest(155,
            new(general.Id, "General", 50m, 150),
            new(vip.Id, "VIP", 120m, 5)));

        saved().Single(t => t.Id == vip.Id).Capacity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_With_Unknown_Tier_Id_Throws_PricingTierNotFound()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);

        await _sut.Invoking(s => s.UpdateAsync(evt.Id, UpdateRequest(220,
                new(general.Id, "General", 50m, 150),
                new(vip.Id, "VIP", 120m, 50),
                new(Guid.NewGuid(), "Phantom", 10m, 20))))
            .Should().ThrowAsync<PricingTierNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_With_Duplicate_Tier_Id_Throws_ArgumentException()
    {
        var evt = GivenEvent();
        var (general, vip) = (evt.PricingTiers[0], evt.PricingTiers[1]);

        await _sut.Invoking(s => s.UpdateAsync(evt.Id, UpdateRequest(250,
                new(general.Id, "General", 50m, 150),
                new(general.Id, "General Again", 50m, 50),
                new(vip.Id, "VIP", 120m, 50))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*more than once*");
    }

    [Fact]
    public async Task DeleteAsync_WhenEventNotFound_ThrowsEventNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdWithTiersAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        await _sut.Invoking(s => s.DeleteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenEventHasNoSales_CallsRepositoryDelete()
    {
        var evt = GivenEvent(generalSold: 0, vipSold: 0);

        await _sut.DeleteAsync(evt.Id);

        _repoMock.Verify(r => r.DeleteAsync(evt.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenEventHasSales_Throws_And_Deletes_Nothing()
    {
        var evt = GivenEvent();

        await _sut.Invoking(s => s.DeleteAsync(evt.Id))
            .Should().ThrowAsync<SalesHistoryConflictException>().WithMessage("*15 tickets sold*");

        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    private Event GivenEvent(int generalSold = 10, int vipSold = 5)
    {
        var evt = SampleEvent(Guid.NewGuid());
        evt.PricingTiers[0].TicketsSold = generalSold;
        evt.PricingTiers[1].TicketsSold = vipSold;
        _repoMock.Setup(r => r.GetByIdWithTiersAsync(evt.Id)).ReturnsAsync(evt);
        return evt;
    }

    private Func<List<PricingTier>> CaptureSavedTiers()
    {
        List<PricingTier> saved = [];
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<IEnumerable<PricingTier>>()))
            .Callback((Event _, IEnumerable<PricingTier> tiers) => saved = tiers.ToList())
            .Returns(Task.CompletedTask);
        return () => saved;
    }

    private static UpdateEventRequest UpdateRequest(int totalCapacity, params UpdatePricingTierRequest[] tiers) => new(
        "Rock Night", "An amazing night", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), new TimeOnly(20, 0),
        totalCapacity, [.. tiers]);

    private static Event SampleEvent(Guid id) => new()
    {
        Id = id,
        Name = "Rock Night",
        Description = "An amazing night",
        Venue = "The Arena",
        Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        Time = new TimeOnly(20, 0),
        TotalCapacity = 200,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        PricingTiers =
        [
            new() { Id = Guid.NewGuid(), Name = "General", Price = 50m, Capacity = 150, TicketsSold = 10 },
            new() { Id = Guid.NewGuid(), Name = "VIP", Price = 120m, Capacity = 50, TicketsSold = 5 }
        ]
    };
}
