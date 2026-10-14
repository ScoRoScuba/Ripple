using Ripple.Api.Cors;
using Ripple.Api.Documentation;
using Ripple.Api.Endpoints;
using Ripple.Api.Logging;
using Ripple.Api.Middleware;
using Ripple.Api.Validators;
using Ripple.Api.Versioning;
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
        .AddVersioning()
        .AddEndpoints(typeof(IEndpoint).Assembly)
        .AddApiDocumentation()
        .AddCorsPolicy(builder.Configuration);

    var app = builder.Build();

    // Outermost, so the request log records the status code the exception middleware sets.
    app.UseRequestLogging();
    // Early, so preflight (OPTIONS) requests are answered before the rest of the pipeline runs.
    app.UseCorsPolicy();
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
