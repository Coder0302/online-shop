using ECommerce.App;
using ECommerce.App.UseCases;
using ECommerce.Data;
using ECommerce.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using System.Diagnostics;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data;
using Swashbuckle.AspNetCore.Swagger;
using System.Text;
using ECommerce.Controller;

var cs = AppConfig.ConnectionStringPg;
var optBuilder = new DbContextOptionsBuilder<ECommerceDbContext>()
    .UseNpgsql(cs, o => o.EnableRetryOnFailure());

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using var db = new ECommerceDbContext(optBuilder.Options);

foreach (var arg in args)
if (arg == "seed" || arg == "seed_new")
{
    // if (arg == "seed_new")
    // Console.WriteLine("Applying schema fixes/migrations...");
    // await InitAndFixes.ApplyAsync(db);
    try
    {
        Console.WriteLine("Seeding data (this can take a while for millions of rows)...");
        var seed = new SeedRunner(cs, db);
        var seedOptions = new SeedOptions(
            Brands: 1000,
            Categories: 100,
            Products: 5000,
            VariantsPerProductMin: 1,
            VariantsPerProductMax: 3,
            Warehouses: 5,
            PriceLists: 2,
            Customers: 15_000,
            TargetCartItems: 3_200_000, // 3–3.5 млн — регулируется тут
            MaxItemsPerCart: 12
        );
        //await seed.RunAsync(seedOptions);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    Console.WriteLine("Начинаем тесты");
    Stopwatch sw;
    try
    {
        var demo = new AggregateAndWindowsDemo(db);
        Console.WriteLine("Агрегаторы начало");
        sw = Stopwatch.StartNew();
        /// Агрегаторы
        await demo.ActiveProductsByCategoryAsync();
        await demo.SkusPerProductAsync();
        await demo.AvailableStockPerVariantAsync();
        await demo.AvgCurrentPriceByBrandAsync("RRP");
        await demo.CartTotalsByCustomerAsync();
        Console.WriteLine($"Агрегаторы конец: {sw.ElapsedMilliseconds} мс.");

        Console.WriteLine("Оконные начало");
        sw = Stopwatch.StartNew();
        /// Оконные
        await demo.PriceRankWithinCategoryAsync("RRP");
        await demo.LatestPricePerVariantAsync("RRP");
        await demo.RunningCartValuePerCustomerAsync();
        Console.WriteLine($"Оконные конец: {sw.ElapsedMilliseconds} мс.");

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    try
    {
        Console.WriteLine("Joins начало");
        sw = Stopwatch.StartNew();
        await JoinsDemo.RunAsync(db);
        Console.WriteLine($"Joins конец: {sw.ElapsedMilliseconds} мс.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    Console.WriteLine("Done.");
}

var settings = JsonWriterSettings.Defaults.Clone();
settings.Indent = true;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Configuration["PG_CONN"]);
Console.WriteLine(builder.Configuration["MG_CONN"]);
var con_set = MongoClientSettings.FromConnectionString(builder.Configuration["MG_CONN"]);
var mgcl = new MongoClient(con_set);
var products = mgcl.GetDatabase("shop").GetCollection<BsonDocument>("brands");

var allprod = products.Find(new BsonDocument()).ToList();
Console.WriteLine(allprod.First().ToJson(settings));
var redis = ConnectionMultiplexer.Connect(builder.Configuration["RS_CONN"]);
var redisdb = redis.GetDatabase();

// Запись данных
redisdb.StringSet("mykey", "Hello from C#");

// Чтение данных
string value = redisdb.StringGet("mykey");
Console.WriteLine(value);
builder.Services.AddDbContextPool<ECommerceDbContext>(sp =>
{
    sp.UseNpgsql(builder.Configuration["PG_CONN"]);
});
builder.Services.AddScoped<JsonWriterSettings>();
builder.Services.AddScoped<ECommerceDbContext>();
builder.Services.AddScoped<ShopController>();
builder.Services.AddSingleton<IMongoClient>(mgcl);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddScoped( sp =>
{
    return sp.GetRequiredService<IMongoClient>().GetDatabase("shop");
});
builder.Services.AddScoped(sp =>
{
    return sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "abc API",
        Version = "v1",
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();