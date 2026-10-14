using FluentValidation;
using Ripple.Api.Endpoints;
using Ripple.Api.Logging;
using Ripple.Api.Middleware;
using Ripple.Api.Validators;
using Ripple.DataStore;
using Ripple.Events;
using Ripple.Reports;
using Ripple.Tickets;
using Serilog;

Log.Logger = SerilogExtensions.CreateStartupLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDataStore(connectionString);

    builder.Services.AddScoped<IEventService, EventService>();
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<IReportService, ReportService>();

    builder.Services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();
    builder.Services.AddScoped<IValidator<UpdateEventRequest>, UpdateEventRequestValidator>();
    builder.Services.AddScoped<IValidator<PurchaseTicketRequest>, PurchaseTicketRequestValidator>();

    builder.Services.AddEndpoints(typeof(Program).Assembly);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Ripple Ticketing API", Version = "v1" });
    });

    var app = builder.Build();

    // Outermost, so the request log records the status code the exception middleware sets.
    app.UseRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ripple Ticketing API v1"));

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

public partial class Program { }
