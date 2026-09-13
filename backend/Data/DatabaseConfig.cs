using Npgsql;

namespace QuantLab.Api.Data;

public static class DatabaseConfig
{
    public static string GetConnectionString()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? throw new InvalidOperationException("DATABASE_URL environment variable is not set.");

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            Database = uri.AbsolutePath.TrimStart('/'),
        };

        return builder.ConnectionString;
    }
}
