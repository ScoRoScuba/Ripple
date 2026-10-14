using Serilog;

namespace Ripple.Api.Logging;

public static class SerilogExtensions
{
    private const string StartupOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Console-only logger used until the host is built, so start-up failures are still captured.
    /// </summary>
    public static Serilog.ILogger CreateStartupLogger() =>
        new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: StartupOutputTemplate)
            .CreateLogger();

    /// <summary>
    /// Replaces the default logging providers with Serilog. Levels and sinks come from the
    /// "Serilog" configuration section; enrichers are fixed here so every environment gets them.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();

        // preserveStaticLogger: each host owns its logger instead of freezing the static Log.Logger,
        // which lets several WebApplicationFactory instances run in parallel in the API tests.
        builder.Services.AddSerilog((services, config) => config
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithClientIp()
                .Enrich.WithCorrelationId(addValueIfHeaderAbsence: true),
            preserveStaticLogger: true);

        return builder;
    }

    /// <summary>
    /// One summary line per HTTP request, written through the host's logger. Serilog's middleware
    /// defaults to the static Log.Logger, which is only the start-up logger here.
    /// </summary>
    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>());

        return app;
    }
}
