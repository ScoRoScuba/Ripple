using DbUp;
using DbUp.Engine;

namespace Ripple.Migrations;

public static class MigrationRunner
{
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        return DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();
    }

    public static bool IsUpgradeRequired(string connectionString)
    {
        // The journal query fails if the database doesn't exist yet, so create it first.
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        return DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .WithTransactionPerScript()
            .Build()
            .IsUpgradeRequired();
    }
}
