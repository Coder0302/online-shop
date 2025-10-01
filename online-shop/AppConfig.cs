namespace ECommerce.App;
public static class AppConfig
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("PG_CONN") ??
        "Host=localhost;Port=5432;Database=shop;Username=pg;Password=pg";
}