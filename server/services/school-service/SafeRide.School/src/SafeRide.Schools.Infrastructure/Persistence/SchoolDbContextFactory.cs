using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SafeRide.Schools.Infrastructure.Persistence;

// Lets `dotnet ef` create the DbContext without running the whole app.
public sealed class SchoolDbContextFactory : IDesignTimeDbContextFactory<SchoolDbContext>
{
    public SchoolDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=SafeRide_School_Db;User Id=sa;Password=SafeRide@123;TrustServerCertificate=True"
            )
            .Options;
        return new SchoolDbContext(options);
    }
}
