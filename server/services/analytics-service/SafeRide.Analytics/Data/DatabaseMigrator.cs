using System.Reflection;
using DbUp;

namespace SafeRide.Analytics.Data;

public static class DatabaseMigrator
{
    public static void Run(IConfiguration config)
    {
        var connectionString =
            config.GetConnectionString("Analytics")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Analytics is not configured."
            );

        EnsureDatabase.For.SqlDatabase(connectionString);

        // DbUp keeps a SchemaVersions journal, runs each script exactly once in
        // filename order, and takes a lock — so replicas starting at the same
        // moment cannot race each other.
        var upgrader = DeployChanges
            .To.SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                // Only the Migrations folder. The query scripts under Data\Sql
                // are embedded in the same assembly, and without this filter
                // DbUp would happily run an UPDATE statement as a migration.
                name => name.Contains(".Migrations.", StringComparison.Ordinal)
            )
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            // Refuse to start. A service running against a half-migrated schema
            // produces wrong reports rather than obvious errors.
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }
}
