namespace ECommerce.App;

public static class AppConfig
{
    private const string DefaultPgConnection =
        "Host=localhost;Port=5432;Database=shop;Username=pg;Password=pg";

    private const string DefaultMongoConnection = "mongodb://localhost:27017";
    private const string DefaultRedisConnection = "localhost:6379";

    /// <summary>
    /// PostgreSQL connection string for EF Core.
    /// </summary>
    public static string ResolvePg(IConfiguration configuration) =>
        ResolveConnectionString(configuration, "PG_CONN", DefaultPgConnection);

    /// <summary>
    /// MongoDB connection string for product read-side storage.
    /// </summary>
    public static string ResolveMongo(IConfiguration configuration) =>
        ResolveConnectionString(configuration, "MG_CONN", DefaultMongoConnection);

    /// <summary>
    /// Redis connection string for caching.
    /// </summary>
    public static string ResolveRedis(IConfiguration configuration) =>
        ResolveConnectionString(configuration, "RS_CONN", DefaultRedisConnection);

    private static string ResolveConnectionString(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var fromConfiguration = configuration[key];
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
        {
            return fromConfiguration;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        return fallback;
    }
}
