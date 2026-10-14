using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ripple.Api.Endpoints;
using Ripple.Api.Middleware;
using Ripple.Api.Validators;
using Ripple.Api.Versioning;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;

namespace Ripple.ApiTests.Scaffolding;

/// <summary>
/// Test-only startup: the API's endpoints, versioning, validators and exception handling, with the
/// services mocked. Nothing else from the real application is wired in.
/// </summary>
public class ScenarioStartup<TApplicationSource>
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddValidators();
        services.AddVersioning();
        services.AddEndpoints(typeof(TApplicationSource).Assembly);

        services.AddMock<IEventService>();
        services.AddMock<ITicketService>();
        services.AddMock<IReportService>();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapEndpoints());
    }
}
