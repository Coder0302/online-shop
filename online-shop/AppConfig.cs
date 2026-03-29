namespace ECommerce.App;

public static class AppConfig
{
    private const string DefaultPgConnection =
        "Host=localhost;Port=5432;Database=shop;Username=pg;Password=pg";

    private const string DefaultMongoConnection = "mongodb://localhost:27017";
    private const string DefaultRedisConnection = "localhost:6379";
    private const string DefaultNeo4jUrl = "bolt://localhost:7687";
    private const string DefaultNeo4jUser = "neo4j";
    private const string DefaultNeo4jPassword = "password";

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

    public static Neo4jSettings ResolveNeo4j(IConfiguration configuration)
    {
        var combined = configuration["NEO4J_CONN"] ?? Environment.GetEnvironmentVariable("NEO4J_CONN");
        if (!string.IsNullOrWhiteSpace(combined))
        {
            var parsed = ParseCompositeConnectionString(combined);
            return new Neo4jSettings(
                parsed.TryGetValue("Url", out var url) && !string.IsNullOrWhiteSpace(url) ? url : DefaultNeo4jUrl,
                parsed.TryGetValue("User", out var user) && !string.IsNullOrWhiteSpace(user) ? user : DefaultNeo4jUser,
                parsed.TryGetValue("Password", out var password) && !string.IsNullOrWhiteSpace(password) ? password : DefaultNeo4jPassword);
        }

        return new Neo4jSettings(
            ResolveConnectionString(configuration, "NEO4J_URL", DefaultNeo4jUrl),
            ResolveConnectionString(configuration, "NEO4J_USER", DefaultNeo4jUser),
            ResolveConnectionString(configuration, "NEO4J_PASSWORD", DefaultNeo4jPassword));
    }

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

    private static IReadOnlyDictionary<string, string> ParseCompositeConnectionString(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
            {
                continue;
            }

            var key = segment[..separatorIndex];
            var segmentValue = segment[(separatorIndex + 1)..];
            result[key] = segmentValue;
        }

        return result;
    }
}

public sealed record Neo4jSettings(string Url, string User, string Password);
