using Domus.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Domus.Api.Tests;

public sealed class DatabaseConnectionTests
{
    [Fact]
    public void Resolve_ConvertsPostgresUrlInConnectionStringsDatabase()
    {
        var configuration = Configuration(
            ("ConnectionStrings:Database", "postgres://domus:secret@host.docker.internal:5432/domus"));

        var builder = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration));

        Assert.Equal("host.docker.internal", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("domus", builder.Username);
        Assert.Equal("secret", builder.Password);
        Assert.Equal("domus", builder.Database);
    }

    [Fact]
    public void Resolve_ConvertsPostgresqlUrlFromDatabaseUrl()
    {
        var configuration = Configuration(
            ("DATABASE_URL", "postgresql://domus:secret@db.example:6543/app"));

        var builder = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration));

        Assert.Equal("db.example", builder.Host);
        Assert.Equal(6543, builder.Port);
        Assert.Equal("app", builder.Database);
    }

    [Fact]
    public void Resolve_LeavesAdoNetConnectionStringUnchanged()
    {
        const string ado = "Host=localhost;Database=domus;Username=domus;Password=domus";
        var configuration = Configuration(("ConnectionStrings:Database", ado));

        var builder = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration));

        Assert.Equal("localhost", builder.Host);
        Assert.Equal("domus", builder.Database);
        Assert.Equal("domus", builder.Username);
    }

    [Fact]
    public void Resolve_PrefersConnectionStringsDatabaseOverDatabaseUrl()
    {
        var configuration = Configuration(
            ("ConnectionStrings:Database", "postgres://from-cs:pw@cs-host:5432/csdb"),
            ("DATABASE_URL", "postgres://from-url:pw@url-host:5432/urldb"));

        var builder = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration));

        Assert.Equal("cs-host", builder.Host);
        Assert.Equal("csdb", builder.Database);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
