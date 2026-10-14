using Microsoft.Extensions.DependencyInjection;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;

namespace Ripple.Services;

public static class ServicesServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
