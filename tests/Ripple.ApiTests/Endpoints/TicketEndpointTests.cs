using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Api.Endpoints;
using Ripple.ApiTests.Scaffolding;
using Ripple.Tickets;
using Ripple.Domain.Exceptions;

namespace Ripple.ApiTests.Endpoints;

public class TicketEndpointTests : IClassFixture<CustomWebApplicationFactory<ScenarioStartup<IEndpoint>>>
{
    private readonly HttpClient _client;
    private readonly Mock<ITicketService> _ticketService;

    public TicketEndpointTests(CustomWebApplicationFactory<ScenarioStartup<IEndpoint>> factory)
    {
        _client = factory.CreateClient();
        _ticketService = factory.Services.ResetMock<ITicketService>();
    }

    [Fact]
    public async Task GetAvailability_WhenFound_Returns200()
    {
        var eventId = Guid.NewGuid();
        _ticketService.Setup(s => s.GetAvailabilityAsync(eventId))
            .ReturnsAsync(SampleAvailability(eventId));

        var response = await _client.GetAsync($"/api/events/{eventId}/availability");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventAvailabilityResponse>();
        body!.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task GetAvailability_WhenEventNotFound_Returns404()
    {
        var eventId = Guid.NewGuid();
        _ticketService.Setup(s => s.GetAvailabilityAsync(eventId))
            .ThrowsAsync(new EventNotFoundException(eventId));

        var response = await _client.GetAsync($"/api/events/{eventId}/availability");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PurchaseTickets_WithValidRequest_Returns201()
    {
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        _ticketService.Setup(s => s.PurchaseAsync(eventId, It.IsAny<PurchaseTicketRequest>()))
            .ReturnsAsync(SamplePurchaseResponse(eventId, tierId));

        var request = new PurchaseTicketRequest(tierId, 2, "Jane Doe", "jane@example.com");
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PurchaseTickets_WhenCapacityExceeded_Returns409()
    {
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        _ticketService.Setup(s => s.PurchaseAsync(eventId, It.IsAny<PurchaseTicketRequest>()))
            .ThrowsAsync(new InsufficientCapacityException(1, 5));

        var request = new PurchaseTicketRequest(tierId, 5, "Jane Doe", "jane@example.com");
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PurchaseTickets_WithInvalidEmail_Returns422()
    {
        var eventId = Guid.NewGuid();
        var request = new PurchaseTicketRequest(Guid.NewGuid(), 2, "Jane Doe", "not-an-email");

        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PurchaseTickets_WithZeroQuantity_Returns422()
    {
        var eventId = Guid.NewGuid();
        var request = new PurchaseTicketRequest(Guid.NewGuid(), 0, "Jane Doe", "jane@example.com");

        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static EventAvailabilityResponse SampleAvailability(Guid eventId) => new(
        eventId, "Rock Night", 200, 10, 190,
        [new(Guid.NewGuid(), "General", 50m, 200, 10, 190)]);

    private static TicketPurchaseResponse SamplePurchaseResponse(Guid eventId, Guid tierId) => new(
        Guid.NewGuid(), eventId, tierId, "General", 50m, 2, 100m, "Jane Doe", "jane@example.com", DateTime.UtcNow);
}
