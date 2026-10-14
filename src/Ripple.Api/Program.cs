using Ripple.Api.Documentation;
using Ripple.Api.Endpoints;
using Ripple.Api.Logging;
using Ripple.Api.Middleware;
using Ripple.Api.Validators;
using Ripple.DataStore;
using Ripple.Services;
using Serilog;

Log.Logger = SerilogExtensions.CreateStartupLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services
        .AddDataStore(connectionString)
        .AddServices()
        .AddValidators()
        .AddEndpoints(typeof(IEndpoint).Assembly)
        .AddApiDocumentation();

    var app = builder.Build();

    // Outermost, so the request log records the status code the exception middleware sets.
    app.UseRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseApiDocumentation();

    app.MapEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "[Program]:[Startup] - Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
