using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Api.Endpoints;
using Ripple.ApiTests.Scaffolding;
using Ripple.Events;
using Ripple.Domain.Exceptions;

namespace Ripple.ApiTests.Endpoints;

public class EventEndpointTests : IClassFixture<CustomWebApplicationFactory<ScenarioStartup<IEndpoint>>>
{
    private readonly HttpClient _client;
    private readonly Mock<IEventService> _eventService;

    public EventEndpointTests(CustomWebApplicationFactory<ScenarioStartup<IEndpoint>> factory)
    {
        _client = factory.CreateClient();
        _eventService = factory.Services.ResetMock<IEventService>();
    }

    [Fact]
    public async Task GetAllEvents_Returns200WithList()
    {
        _eventService.Setup(s => s.GetAllAsync())
            .ReturnsAsync([SampleEventResponse(), SampleEventResponse()]);

        var response = await _client.GetAsync("/api/v1/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvent_WhenFound_Returns200()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(SampleEventResponse(id));

        var response = await _client.GetAsync($"/api/v1/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        body!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetEvent_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.GetByIdAsync(id)).ThrowsAsync(new EventNotFoundException(id));

        var response = await _client.GetAsync($"/api/v1/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_Returns201()
    {
        var created = SampleEventResponse();
        _eventService.Setup(s => s.CreateAsync(It.IsAny<CreateEventRequest>())).ReturnsAsync(created);

        var response = await _client.PostAsJsonAsync("/api/v1/events", SampleCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/events/{created.Id}");
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns422()
    {
        var request = new CreateEventRequest(
            "", "desc", "venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 100,
            [new("General", 50m, 100)]);

        var response = await _client.PostAsJsonAsync("/api/v1/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns422()
    {
        var request = new CreateEventRequest(
            "Rock Night", "desc", "venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(20, 0), 100,
            [new("General", 50m, 100)]);

        var response = await _client.PostAsJsonAsync("/api/v1/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateEvent_WithValidRequest_Returns200()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateEventRequest>()))
            .ReturnsAsync(SampleEventResponse(id));

        var response = await _client.PutAsJsonAsync($"/api/v1/events/{id}", SampleUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEvent_WithNoPricingTiers_Returns422()
    {
        var request = SampleUpdateRequest() with { PricingTiers = [] };

        var response = await _client.PutAsJsonAsync($"/api/v1/events/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateEvent_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateEventRequest>()))
            .ThrowsAsync(new EventNotFoundException(id));

        var response = await _client.PutAsJsonAsync($"/api/v1/events/{id}", SampleUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_WhenFound_Returns204()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync($"/api/v1/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEvent_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.DeleteAsync(id)).ThrowsAsync(new EventNotFoundException(id));

        var response = await _client.DeleteAsync($"/api/v1/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static EventResponse SampleEventResponse(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Rock Night", "An amazing show", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), new TimeOnly(20, 0), 200,
        [new(Guid.NewGuid(), "General", 50m, 150, 10, 140)],
        DateTime.UtcNow, DateTime.UtcNow);

    private static CreateEventRequest SampleCreateRequest() => new(
        "Rock Night", "An amazing show", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        new TimeOnly(20, 0), 200,
        [new("General", 50m, 150), new("VIP", 120m, 50)]);

    private static UpdateEventRequest SampleUpdateRequest() => new(
        "Rock Night (Rescheduled)", "An amazing show", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
        new TimeOnly(20, 0), 200,
        [new("General", 50m, 200)]);
}
