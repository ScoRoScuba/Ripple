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

    public EventServiceTests() => _sut = new EventService(_repoMock.Object);

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
    public async Task DeleteAsync_WhenEventNotFound_ThrowsEventNotFoundException()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.DeleteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenEventExists_CallsRepositoryDelete()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        await _sut.DeleteAsync(id);

        _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }

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
