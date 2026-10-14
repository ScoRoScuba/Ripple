using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ripple.ApiTests.Scaffolding;

/// <summary>
/// Hosts <typeparamref name="TStartup"/> on a TestServer instead of the API's Program, so each
/// scenario wires only what it exercises.
/// </summary>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseStartup<TStartup>()
                .UseTestServer());

    // The factory guesses the content root as <solution>/<assembly name>, which doesn't exist
    // because test projects live under tests/. Nothing is served from disk, so use the output folder.
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseContentRoot(AppContext.BaseDirectory);
}
