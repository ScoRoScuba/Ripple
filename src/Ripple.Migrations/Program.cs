using System.Reflection;
using DbUp;

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? args.FirstOrDefault()
    ?? "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres";

EnsureDatabase.For.PostgresqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

if (upgrader.IsUpgradeRequired())
{
    Console.WriteLine("Applying pending migrations...");
}
else
{
    Console.WriteLine("Database is already up to date.");
}

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(result.Error);
    Console.ResetColor();
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("All migrations applied successfully.");
Console.ResetColor();
return 0;
