using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Endpoints;
using Ripple.Api.Versioning;
using Ripple.ApiTests.Scaffolding;

namespace Ripple.ApiTests.OpenApiTests;

public class OpenApiDocumentTests : IClassFixture<CustomWebApplicationFactory<OpenApiScenarioStartup<IEndpoint>>>
{
    private readonly CustomWebApplicationFactory<OpenApiScenarioStartup<IEndpoint>> _factory;
    private readonly HttpClient _client;

    public OpenApiDocumentTests(CustomWebApplicationFactory<OpenApiScenarioStartup<IEndpoint>> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Document_Is_Generated_For_Every_Api_Version()
    {
        foreach (var version in ApiVersions.All)
        {
            var response = await _client.GetAsync($"/swagger/v{version.MajorVersion}/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"the v{version.MajorVersion} document should generate");
        }
    }

    [Fact]
    public async Task V1_Document_Lists_Every_Mapped_Route_Under_Its_Versioned_Path()
    {
        var expectedRouteCount = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText)
            .Distinct()
            .Count();

        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        var paths = JsonDocument.Parse(json).RootElement.GetProperty("paths")
            .EnumerateObject().Select(p => p.Name).ToList();

        paths.Should().HaveCount(expectedRouteCount);
        paths.Should().OnlyContain(path => path.StartsWith("/api/v1/"));
    }
}
