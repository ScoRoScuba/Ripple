using FluentAssertions;
using Moq;
using Ripple.Events;
using Ripple.Tickets;
using Ripple.Domain.Entities;
using Ripple.Domain.Exceptions;

namespace Ripple.UnitTests.Services;

public class TicketServiceTests
{
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly TicketService _sut;

    public TicketServiceTests() =>
        _sut = new TicketService(_eventRepoMock.Object, _ticketRepoMock.Object);

    [Fact]
    public async Task PurchaseAsync_WhenEventNotFound_ThrowsEventNotFoundException()
    {
        _eventRepoMock.Setup(r => r.GetByIdWithTiersAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        await _sut.Invoking(s => s.PurchaseAsync(Guid.NewGuid(), SampleRequest(Guid.NewGuid())))
            .Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task PurchaseAsync_WhenTierNotFound_ThrowsPricingTierNotFoundException()
    {
        var eventId = Guid.NewGuid();
        var unknownTierId = Guid.NewGuid();

        _eventRepoMock.Setup(r => r.GetByIdWithTiersAsync(eventId)).ReturnsAsync(SampleEvent(eventId));

        await _sut.Invoking(s => s.PurchaseAsync(eventId, SampleRequest(unknownTierId)))
            .Should().ThrowAsync<PricingTierNotFoundException>();
    }

    [Fact]
    public async Task PurchaseAsync_WhenValid_ReturnsTicketResponse()
    {
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        var evt = SampleEvent(eventId, tierId);

        _eventRepoMock.Setup(r => r.GetByIdWithTiersAsync(eventId)).ReturnsAsync(evt);
        _ticketRepoMock.Setup(r => r.PurchaseAsync(eventId, tierId, 2, "Jane Doe", "jane@example.com", 50m))
            .ReturnsAsync(new Ticket
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                PricingTierId = tierId,
                PurchaserName = "Jane Doe",
                PurchaserEmail = "jane@example.com",
                Quantity = 2,
                UnitPrice = 50m,
                PurchasedAt = DateTime.UtcNow
            });

        var result = await _sut.PurchaseAsync(eventId, SampleRequest(tierId));

        result.Quantity.Should().Be(2);
        result.TotalPrice.Should().Be(100m);
        result.PurchaserName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetAvailabilityAsync_WhenEventNotFound_ThrowsEventNotFoundException()
    {
        _eventRepoMock.Setup(r => r.GetByIdWithTiersAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        await _sut.Invoking(s => s.GetAvailabilityAsync(Guid.NewGuid()))
            .Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task GetAvailabilityAsync_ReturnsSummedTotals()
    {
        var eventId = Guid.NewGuid();
        var evt = SampleEvent(eventId);

        _eventRepoMock.Setup(r => r.GetByIdWithTiersAsync(eventId)).ReturnsAsync(evt);

        var result = await _sut.GetAvailabilityAsync(eventId);

        result.TotalTicketsSold.Should().Be(15);
        result.TotalAvailable.Should().Be(185);
        result.Tiers.Should().HaveCount(2);
    }

    private static PurchaseTicketRequest SampleRequest(Guid tierId) =>
        new(tierId, 2, "Jane Doe", "jane@example.com");

    private static Event SampleEvent(Guid eventId, Guid? tierId = null) => new()
    {
        Id = eventId,
        Name = "Rock Night",
        Venue = "The Arena",
        Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        Time = new TimeOnly(20, 0),
        TotalCapacity = 200,
        PricingTiers =
        [
            new() { Id = tierId ?? Guid.NewGuid(), EventId = eventId, Name = "General", Price = 50m, Capacity = 150, TicketsSold = 10 },
            new() { Id = Guid.NewGuid(), EventId = eventId, Name = "VIP", Price = 120m, Capacity = 50, TicketsSold = 5 }
        ]
    };
}
