using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Endpoints;
using Ripple.ApiTests.Scaffolding;

namespace Ripple.ApiTests.Endpoints;

public class EndpointRegistrationTests(CustomWebApplicationFactory<ScenarioStartup<IEndpoint>> factory) : IClassFixture<CustomWebApplicationFactory<ScenarioStartup<IEndpoint>>>
{
    [Fact]
    public void Every_IEndpoint_Implementation_Is_Mapped()
    {
        _ = factory.CreateClient(); // builds the host

        var endpointTypes = typeof(IEndpoint).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .ToList();

        var mappedHandlerTypes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.Metadata.GetMetadata<System.Reflection.MethodInfo>()?.DeclaringType)
            .ToHashSet();

        endpointTypes.Should().NotBeEmpty();
        mappedHandlerTypes.Should().Contain(endpointTypes);
    }
}
