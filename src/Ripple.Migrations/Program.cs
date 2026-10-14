using Ripple.Migrations;
using Ripple.Migrations.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? args.FirstOrDefault()
        ?? "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres";

    var dbUpLog = new SerilogUpgradeLog(Log.ForContext("SourceContext", "DbUp"));

    MigrationRunner.EnsureDatabaseExists(connectionString, dbUpLog);

    if (MigrationRunner.IsUpgradeRequired(connectionString))
        Log.Information("[Migrations]:[Main] - Applying pending migrations");
    else
        Log.Information("[Migrations]:[Main] - Database is already up to date");

    var result = MigrationRunner.Run(connectionString, dbUpLog);

    if (!result.Successful)
    {
        Log.Error(result.Error, "[Migrations]:[Main] - Migration failed on script {ScriptName}", result.ErrorScript?.Name);
        return 1;
    }

    Log.Information("[Migrations]:[Main] - {ScriptCount} migration script(s) applied successfully", result.Scripts.Count());
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "[Migrations]:[Main] - Migration run terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
