namespace ECommerce.App;
public static class AppConfig
{
    public static string ConnectionStringPg =>
        Environment.GetEnvironmentVariable("PG_CONN") ??
        "Host=localhost;Port=5432;Database=shop;Username=pg;Password=pg";
    public static string ConnectionStringMongo =>
        Environment.GetEnvironmentVariable("MG_CONN") ??
        "mongodb://localhost:27017";
    public static string ConnectionStringRedis =>
        Environment.GetEnvironmentVariable("RS_CONN") ??
        "";
}