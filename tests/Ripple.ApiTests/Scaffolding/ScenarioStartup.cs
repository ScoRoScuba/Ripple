using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Endpoints;
using Ripple.Api.Middleware;
using Ripple.Api.Validators;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;

namespace Ripple.ApiTests.Scaffolding;

/// <summary>
/// Test-only startup: the API's endpoints, validators and exception handling, with the
/// services mocked. Nothing else from the real application is wired in.
/// </summary>
public class ScenarioStartup<TApplicationSource>
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddValidators();
        services.AddEndpoints(typeof(TApplicationSource).Assembly);

        services.AddMock<IEventService>();
        services.AddMock<ITicketService>();
        services.AddMock<IReportService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapEndpoints());
    }
}
