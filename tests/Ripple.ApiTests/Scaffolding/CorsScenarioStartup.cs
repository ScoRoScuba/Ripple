using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Cors;

namespace Ripple.ApiTests.Scaffolding;

/// <summary>
/// <see cref="ScenarioStartup{TApplicationSource}"/> plus the CORS policy, configured to allow
/// <see cref="AllowedOrigin"/> only. CORS runs ahead of the base pipeline, as in the real one.
/// </summary>
public class CorsScenarioStartup<TApplicationSource> : ScenarioStartup<TApplicationSource>
{
    public const string AllowedOrigin = "https://app.example.com";

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Configured with a trailing slash on purpose: browsers never send one, so the
                // policy must normalise it for the tests' Origin header to match.
                [$"{CorsExtensions.SectionName}:AllowedOrigins:0"] = AllowedOrigin + "/"
            })
            .Build();

        services.AddCorsPolicy(configuration);
    }

    public override void Configure(IApplicationBuilder app)
    {
        app.UseCorsPolicy();
        base.Configure(app);
    }
}
