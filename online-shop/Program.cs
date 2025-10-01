using ECommerce.App;
using ECommerce.App.UseCases;
using ECommerce.Data;
using ECommerce.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var cs = AppConfig.ConnectionString;
var optBuilder = new DbContextOptionsBuilder<ECommerceDbContext>()
    .UseNpgsql(cs, o => o.EnableRetryOnFailure());

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using var db = new ECommerceDbContext(optBuilder.Options);
//Console.WriteLine("Applying schema fixes/migrations...");
//await InitAndFixes.ApplyAsync(db);

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
