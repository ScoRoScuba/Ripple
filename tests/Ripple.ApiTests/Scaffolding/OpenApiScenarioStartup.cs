using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Documentation;

namespace Ripple.ApiTests.Scaffolding;

/// <summary>
/// <see cref="ScenarioStartup{TApplicationSource}"/> plus the API documentation (Swagger), for tests
/// that check the generated OpenAPI document.
/// </summary>
public class OpenApiScenarioStartup<TApplicationSource> : ScenarioStartup<TApplicationSource>
{
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddApiDocumentation();
    }

    public override void Configure(IApplicationBuilder app)
    {
        app.UseApiDocumentation();
        base.Configure(app);
    }
}
