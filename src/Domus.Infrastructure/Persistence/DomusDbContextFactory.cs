using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Domus.Infrastructure.Persistence;

public sealed class DomusDbContextFactory : IDesignTimeDbContextFactory<DomusDbContext>
{
    public DomusDbContext CreateDbContext(string[] args)
    {
        var raw =
            Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        var connectionString = string.IsNullOrWhiteSpace(raw)
            ? "Host=localhost;Database=domus;Username=domus;Password=domus"
            : DatabaseConnection.Normalize(raw);

        var options = new DbContextOptionsBuilder<DomusDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(DomusDbContext).Assembly.FullName))
            .Options;

        return new DomusDbContext(options);
    }
}
