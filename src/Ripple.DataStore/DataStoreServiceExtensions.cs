using Microsoft.Extensions.DependencyInjection;
using Ripple.Data;
using Ripple.DataStore.Data;
using Ripple.DataStore.Repositories;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;

namespace Ripple.DataStore;

public static class DataStoreServiceExtensions
{
    public static IServiceCollection AddDataStore(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        return services;
    }
}
