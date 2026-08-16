using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Domus.Infrastructure.Persistence;

public static class DatabaseConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return Normalize(connectionString);
        }

        var databaseUrl = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return string.Empty;
        }

        return Normalize(databaseUrl);
    }

    public static string Normalize(string connectionString)
    {
        if (!IsPostgresUrl(connectionString))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Prefer,
        };

        return builder.ConnectionString;
    }

    private static bool IsPostgresUrl(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}
