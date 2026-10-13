using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Events;
using Ripple.Domain.Exceptions;

namespace Ripple.ApiTests.Endpoints;

public class EventEndpointTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllEvents_Returns200WithList()
    {
        factory.MockEventService.Setup(s => s.GetAllAsync())
            .ReturnsAsync([SampleEventResponse(), SampleEventResponse()]);

        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvent_WhenFound_Returns200()
    {
        var id = Guid.NewGuid();
        factory.MockEventService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(SampleEventResponse(id));

        var response = await _client.GetAsync($"/api/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        body!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetEvent_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        factory.MockEventService.Setup(s => s.GetByIdAsync(id)).ThrowsAsync(new EventNotFoundException(id));

        var response = await _client.GetAsync($"/api/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_Returns201()
    {
        var created = SampleEventResponse();
        factory.MockEventService.Setup(s => s.CreateAsync(It.IsAny<CreateEventRequest>())).ReturnsAsync(created);

        var response = await _client.PostAsJsonAsync("/api/events", SampleCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns422()
    {
        var request = new CreateEventRequest(
            "", "desc", "venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            new TimeOnly(20, 0), 100,
            [new("General", 50m, 100)]);

        var response = await _client.PostAsJsonAsync("/api/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns422()
    {
        var request = new CreateEventRequest(
            "Rock Night", "desc", "venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(20, 0), 100,
            [new("General", 50m, 100)]);

        var response = await _client.PostAsJsonAsync("/api/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteEvent_WhenFound_Returns204()
    {
        var id = Guid.NewGuid();
        factory.MockEventService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync($"/api/events/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEvent_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        factory.MockEventService.Setup(s => s.DeleteAsync(id)).ThrowsAsync(new EventNotFoundException(id));

        var response = await _client.DeleteAsync($"/api/events/{id}");

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
}
