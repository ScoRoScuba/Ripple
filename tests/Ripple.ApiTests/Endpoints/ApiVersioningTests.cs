using System.Net;
using FluentAssertions;
using Moq;
using Ripple.Api.Endpoints;
using Ripple.Api.Versioning;
using Ripple.ApiTests.Scaffolding;
using Ripple.Events;

namespace Ripple.ApiTests.Endpoints;

public class ApiVersioningTests : IClassFixture<CustomWebApplicationFactory<ScenarioStartup<IEndpoint>>>
{
    private readonly HttpClient _client;

    public ApiVersioningTests(CustomWebApplicationFactory<ScenarioStartup<IEndpoint>> factory)
    {
        _client = factory.CreateClient();
        factory.Services.ResetMock<IEventService>()
            .Setup(s => s.GetAllAsync()).ReturnsAsync([]);
    }

    [Fact]
    public async Task Response_Reports_Supported_Api_Versions()
    {
        var response = await _client.GetAsync("/api/v1/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("api-supported-versions").Single()
            .Should().Be(string.Join(", ", ApiVersions.All));
    }

    // With URL-segment versioning the version is part of the resource path, so an
    // unsupported version is a resource that doesn't exist: 404, not 400.
    [Fact]
    public async Task Unsupported_Api_Version_Returns404()
    {
        var unsupported = ApiVersions.All.Max(v => v.MajorVersion!.Value) + 1;

        var response = await _client.GetAsync($"/api/v{unsupported}/events");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unversioned_Route_Is_Not_Served()
    {
        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
