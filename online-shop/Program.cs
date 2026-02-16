using ECommerce.App;
using ECommerce.App.UseCases;
using ECommerce.Data;
using ECommerce.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Diagnostics;
<<<<<<< HEAD
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Swashbuckle.AspNetCore.Swagger;
using System.Text;
using ECommerce.Controller;
using Neo4j.Driver;
using project.Services;

var cs = AppConfig.ConnectionStringPg;
var optBuilder = new DbContextOptionsBuilder<ECommerceDbContext>()
    .UseNpgsql(cs, o => o.EnableRetryOnFailure());
=======
>>>>>>> 0394985 (New code)

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.SingleLine = true;
});

var pgConnection = AppConfig.ResolvePg(builder.Configuration);
var mongoConnection = AppConfig.ResolveMongo(builder.Configuration);
var redisConnection = AppConfig.ResolveRedis(builder.Configuration);

builder.Services.AddDbContextPool<ECommerceDbContext>(options =>
{
    options.UseNpgsql(pgConnection, npgsql => npgsql.EnableRetryOnFailure());
});

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("shop"));
builder.Services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Online Shop API",
        Version = "v1"
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowReact",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

if (IsSeedMode(args))
{
    await RunSeedWorkflowAsync(app.Services, app.Logger, pgConnection, app.Lifetime.ApplicationStopping);
    return;
}

app.Logger.LogInformation("Starting API host.");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.MapControllers();
app.Run();

static bool IsSeedMode(IEnumerable<string> args) =>
    args.Any(arg =>
        string.Equals(arg, "seed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "seed_new", StringComparison.OrdinalIgnoreCase));

static async Task RunSeedWorkflowAsync(
    IServiceProvider services,
    ILogger logger,
    string pgConnection,
    CancellationToken cancellationToken)
{
    logger.LogInformation("Seed mode enabled. HTTP host will not start.");

    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

    try
    {
        var seedOptions = new SeedOptions(
            Brands: 1000,
            Categories: 100,
            Products: 5000,
            VariantsPerProductMin: 1,
            VariantsPerProductMax: 3,
            Warehouses: 5,
            PriceLists: 2,
            Customers: 15_000,
            TargetCartItems: 3_200_000,
            MaxItemsPerCart: 12
        );

        logger.LogInformation(
            "Seeding started. Products: {Products}, Customers: {Customers}, TargetCartItems: {TargetCartItems}.",
            seedOptions.Products,
            seedOptions.Customers,
            seedOptions.TargetCartItems);

        var seedTimer = Stopwatch.StartNew();
        var seed = new SeedRunner(pgConnection, db);
        await seed.RunAsync(seedOptions);
        logger.LogInformation("Seeding finished in {ElapsedMs} ms.", seedTimer.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding failed.");
        return;
    }

    try
    {
        await RunDemoQueriesAsync(db, logger, cancellationToken);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Demo query execution failed.");
    }

    logger.LogInformation("Seed workflow completed.");
}

static async Task RunDemoQueriesAsync(
    ECommerceDbContext db,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var demo = new AggregateAndWindowsDemo(db);

    logger.LogInformation("Running aggregate queries.");
    var timer = Stopwatch.StartNew();
    await demo.ActiveProductsByCategoryAsync(cancellationToken);
    await demo.SkusPerProductAsync(cancellationToken);
    await demo.AvailableStockPerVariantAsync(cancellationToken);
    await demo.AvgCurrentPriceByBrandAsync("RRP", ct: cancellationToken);
    await demo.CartTotalsByCustomerAsync(cancellationToken);
    logger.LogInformation("Aggregate queries finished in {ElapsedMs} ms.", timer.ElapsedMilliseconds);

<<<<<<< HEAD
var redis = ConnectionMultiplexer.Connect(builder.Configuration["RS_CONN"]);
var redisdb = redis.GetDatabase();

builder.Services.AddDbContextPool<ECommerceDbContext>(sp =>
{
    sp.UseNpgsql(builder.Configuration["PG_CONN"]);
});

IDriver neo4jDriver = GraphDatabase.Driver(
    builder?.Configuration["Neo4j:Uri"] ?? "bolt://localhost:7687",
    AuthTokens.Basic(
        builder?.Configuration["Neo4j:User"] ?? "neo4j",
        builder?.Configuration["Neo4j:Password"] ?? "password"
    )
);

await using var testSession = neo4jDriver.AsyncSession();
var testResult = await testSession.ExecuteReadAsync(async tx =>
{
    var cursor = await tx.RunAsync("RETURN 1 as test");
    return await cursor.SingleAsync();
});

Console.WriteLine($"Neo4j подключен: {testResult["test"]}");
builder.Services.AddScoped<JsonWriterSettings>();
builder.Services.AddScoped<INeo4jService, Neo4jService>();
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
builder.Services.AddSingleton<IDriver>(neo4jDriver);
builder.Services.AddScoped<INeo4jService, Neo4jService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
=======
    logger.LogInformation("Running window queries.");
    timer.Restart();
    await demo.PriceRankWithinCategoryAsync("RRP", ct: cancellationToken);
    await demo.LatestPricePerVariantAsync("RRP", cancellationToken);
    await demo.RunningCartValuePerCustomerAsync(cancellationToken);
    logger.LogInformation("Window queries finished in {ElapsedMs} ms.", timer.ElapsedMilliseconds);

    logger.LogInformation("Running join demo queries.");
    timer.Restart();
    await JoinsDemo.RunAsync(db, cancellationToken);
    logger.LogInformation("Join demo queries finished in {ElapsedMs} ms.", timer.ElapsedMilliseconds);
}
>>>>>>> 0394985 (New code)
