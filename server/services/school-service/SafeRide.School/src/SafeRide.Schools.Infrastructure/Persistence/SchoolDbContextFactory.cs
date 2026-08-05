using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Infrastructure.Persistence;

// Lets `dotnet ef` create the DbContext without running the whole app.
public sealed class SchoolDbContextFactory : IDesignTimeDbContextFactory<SchoolDbContext>
{
    // Migrations run with no user and no request — "no tenant" is the honest answer.
    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid? TenantId => null;
    }

    public SchoolDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("aaf7daef-ecf1-4c65-8a02-edc910819926")
            .AddEnvironmentVariables()
            .Build();

        var conn =
            config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "No connection string found — set it via user-secrets on the Api project "
                    + "or the ConnectionStrings__Default environment variable."
            );

        var options = new DbContextOptionsBuilder<SchoolDbContext>().UseSqlServer(conn).Options;
        return new SchoolDbContext(options, new DesignTimeTenantProvider());
    }
}
