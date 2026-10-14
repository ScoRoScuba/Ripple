using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;

namespace Ripple.Migrations;

public static class MigrationRunner
{
    public static void EnsureDatabaseExists(string connectionString, IUpgradeLog? log = null) =>
        EnsureDatabase.For.PostgresqlDatabase(connectionString, log ?? new ConsoleUpgradeLog());

    public static bool IsUpgradeRequired(string connectionString) =>
        CreateBuilder(connectionString)
            .Build()
            .IsUpgradeRequired();

    public static DatabaseUpgradeResult Run(string connectionString, IUpgradeLog? log = null) =>
        CreateBuilder(connectionString)
            .LogTo(log ?? new ConsoleUpgradeLog())
            .Build()
            .PerformUpgrade();

    private static UpgradeEngineBuilder CreateBuilder(string connectionString) =>
        DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .WithTransactionPerScript();
}
