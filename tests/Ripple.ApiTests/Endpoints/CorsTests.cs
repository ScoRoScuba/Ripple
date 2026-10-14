using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Ripple.Api.Endpoints;
using Ripple.ApiTests.Scaffolding;
using Ripple.Domain.Exceptions;
using Ripple.Events;

namespace Ripple.ApiTests.Endpoints;

public class CorsTests : IClassFixture<CustomWebApplicationFactory<CorsScenarioStartup<IEndpoint>>>
{
    private const string AllowedOrigin = CorsScenarioStartup<IEndpoint>.AllowedOrigin;
    private const string OtherOrigin = "https://evil.example.com";

    private readonly HttpClient _client;
    private readonly Mock<IEventService> _eventService;

    public CorsTests(CustomWebApplicationFactory<CorsScenarioStartup<IEndpoint>> factory)
    {
        _client = factory.CreateClient();
        _eventService = factory.Services.ResetMock<IEventService>();
    }

    [Fact]
    public async Task Preflight_From_Allowed_Origin_Is_Accepted()
    {
        var response = await _client.SendAsync(Preflight(AllowedOrigin, "POST"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(AllowedOrigin);
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain("POST");
    }

    [Fact]
    public async Task Preflight_From_Other_Origin_Is_Not_Allowed()
    {
        var response = await _client.SendAsync(Preflight(OtherOrigin, "POST"));

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Created_Response_Exposes_Location_And_Version_Headers_To_Browsers()
    {
        _eventService.Setup(s => s.CreateAsync(It.IsAny<CreateEventRequest>())).ReturnsAsync(SampleEvent());

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/events") { Content = JsonContent.Create(SampleCreateRequest()) };
        request.Headers.Add("Origin", AllowedOrigin);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(AllowedOrigin);
        response.Headers.GetValues("Access-Control-Expose-Headers").Single()
            .Should().Contain("Location").And.Contain("api-supported-versions");
    }

    [Fact]
    public async Task Error_Responses_Carry_Cors_Headers()
    {
        var id = Guid.NewGuid();
        _eventService.Setup(s => s.GetByIdAsync(id)).ThrowsAsync(new EventNotFoundException(id));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/events/{id}");
        request.Headers.Add("Origin", AllowedOrigin);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(AllowedOrigin);
    }

    private static HttpRequestMessage Preflight(string origin, string method)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/events");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private static EventResponse SampleEvent() => new(
        Guid.NewGuid(), "Rock Night", "An amazing show", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), new TimeOnly(20, 0), 100,
        [new(Guid.NewGuid(), "General", 50m, 100, 0, 100)],
        DateTime.UtcNow, DateTime.UtcNow);

    private static CreateEventRequest SampleCreateRequest() => new(
        "Rock Night", "An amazing show", "The Arena",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), new TimeOnly(20, 0), 100,
        [new("General", 50m, 100)]);
}
