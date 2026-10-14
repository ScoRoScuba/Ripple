using DbUp;
using DbUp.Builder;
using DbUp.Engine;

namespace Ripple.Migrations;

public static class MigrationRunner
{
    public static void EnsureDatabaseExists(string connectionString) =>
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

    public static bool IsUpgradeRequired(string connectionString) =>
        CreateBuilder(connectionString)
            .Build()
            .IsUpgradeRequired();

    public static DatabaseUpgradeResult Run(string connectionString) =>
        CreateBuilder(connectionString)
            .LogToConsole()
            .Build()
            .PerformUpgrade();

    private static UpgradeEngineBuilder CreateBuilder(string connectionString) =>
        DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .WithTransactionPerScript();
}
