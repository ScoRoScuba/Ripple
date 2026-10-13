using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;
namespace Ripple.ApiTests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public Mock<IEventService> MockEventService { get; } = new();
    public Mock<ITicketService> MockTicketService { get; } = new();
    public Mock<IReportService> MockReportService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove infrastructure registrations so no DB connection is needed
            services.RemoveAll<IEventService>();
            services.RemoveAll<ITicketService>();
            services.RemoveAll<IReportService>();

            services.AddSingleton(MockEventService.Object);
            services.AddSingleton(MockTicketService.Object);
            services.AddSingleton(MockReportService.Object);
        });
    }
}
