using Npgsql;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Entities.Catalog;

namespace ECommerce.Data.Seeding;
public class SeedRunner
{
    private readonly string _conn;
    private readonly ECommerceDbContext _db;
    public SeedRunner(string conn, ECommerceDbContext db) { _conn = conn; _db = db; }

    public async Task RunAsync(SeedOptions opt, CancellationToken ct = default)
    {
        // 1) справочники
        await EnsureBrandsAsync(opt.Brands, ct);
        await EnsureCategoriesAsync(opt.Categories, ct);
        await EnsureProductsAndVariantsAsync(opt.Products, opt.VariantsPerProductMin, opt.VariantsPerProductMax, ct);
        await EnsureWarehousesAsync(opt.Warehouses, ct);
        await EnsureStockAsync(ct);
        await EnsurePriceListsAndPricesAsync(opt.PriceLists, ct);
        await EnsureUsersAndCustomersAsync(opt.Customers, ct);

        // 2) корзины и позиции корзин Ч целимс€ в 3.2 млн строк
        await GenerateCartsAndItemsAsync(
            targetCartItems: opt.TargetCartItems, // напр., 3_200_000
            maxItemsPerCart: opt.MaxItemsPerCart, // напр., 12
            ct: ct);
    }

    private async Task EnsureBrandsAsync(int count, CancellationToken ct)
    {
        if (await _db.Brands.AnyAsync(ct)) return;
        var list = Enumerable.Range(1, count)
            .Select(i => new Entities.Catalog.Brand { BrandId = Guid.NewGuid(), Name = $"Brand-{i}" })
            .ToList();
        await _db.Brands.AddRangeAsync(list, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureCategoriesAsync(int count, CancellationToken ct)
    {
        if (await _db.Categories.AnyAsync(ct)) return;
        var list = Enumerable.Range(1, count)
            .Select(i => new Entities.Catalog.Category { CategoryId = Guid.NewGuid(), Slug = $"cat-{i}", Name = $"Category {i}" })
            .ToList();
        await _db.Categories.AddRangeAsync(list, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureProductsAndVariantsAsync(int products, int vmin, int vmax, CancellationToken ct)
    {
        if (await _db.Products.AnyAsync(ct)) return;

        var rnd = new Random(42);
        var brandIds = await _db.Brands.Select(x => x.BrandId).ToListAsync(ct);
        var catIds = await _db.Categories.Select(x => x.CategoryId).ToListAsync(ct);

        var batchProducts = new List<Entities.Catalog.Product>(products);
        var batchVariants = new List<Entities.Catalog.Variant>(products * ((vmin + vmax) / 2));

        for (int i = 1; i <= products; i++)
        {
            var p = new Entities.Catalog.Product
            {
                ProductId = Guid.NewGuid(),
                BrandId = brandIds[rnd.Next(brandIds.Count)],
                CategoryId = catIds[rnd.Next(catIds.Count)],
                SkuBase = $"SKU-{i:D6}",
                Name = $"Product {i}",
                Status = rnd.NextDouble() < 0.8 ? ProductStatus.Active : ProductStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-rnd.Next(0, 365))
            };
            batchProducts.Add(p);

            var vcount = rnd.Next(vmin, vmax + 1);
            for (int k = 0; k < vcount; k++)
            {
                batchVariants.Add(new Entities.Catalog.Variant
                {
                    VariantId = Guid.NewGuid(),
                    ProductId = p.ProductId,
                    Sku = $"{p.SkuBase}-{k + 1}",
                    OptionKvJson = "{\"color\":\"black\"}",
                    Barcode = $"BAR{rnd.NextInt64():X}",
                    WeightG = rnd.Next(50, 2500),
                    DimensionsMm = new[] { rnd.Next(50, 300), rnd.Next(50, 300), rnd.Next(10, 200) }
                });
            }
        }

        await _db.Products.AddRangeAsync(batchProducts, ct);
        await _db.Variants.AddRangeAsync(batchVariants, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureWarehousesAsync(int count, CancellationToken ct)
    {
        if (await _db.Warehouses.AnyAsync(ct)) return;
        var list = Enumerable.Range(1, count).Select(i => new Entities.Inventory.Warehouse
        {
            WarehouseId = Guid.NewGuid(),
            Code = $"W{i:D2}",
            Name = $"Warehouse {i}",
            CountryCode = "AT"
        }).ToList();
        await _db.Warehouses.AddRangeAsync(list, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureStockAsync(CancellationToken ct)
    {
        if (await _db.StockItems.AnyAsync(ct)) return;
        var rnd = new Random(7);
        var whs = await _db.Warehouses.Select(x => x.WarehouseId).ToListAsync(ct);
        var variants = await _db.Variants.Select(x => x.VariantId).ToListAsync(ct);

        // равномерный разлив остатков
        var rows = new List<Entities.Inventory.StockItem>(variants.Count * Math.Min(3, whs.Count));
        foreach (var v in variants)
        {
            foreach (var w in whs)
            {
                if (rnd.NextDouble() < 0.6) continue; // не в каждом складе лежит
                rows.Add(new Entities.Inventory.StockItem
                {
                    WarehouseId = w,
                    VariantId = v,
                    QtyOnHand = rnd.Next(0, 200),
                    QtyReserved = rnd.Next(0, 30)
                });
            }
        }
        await _db.StockItems.AddRangeAsync(rows, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsurePriceListsAndPricesAsync(int priceLists, CancellationToken ct)
    {
        if (!await _db.PriceLists.AnyAsync(ct))
        {
            var lists = Enumerable.Range(1, priceLists).Select(i => new Entities.Pricing.PriceList
            {
                PriceListId = Guid.NewGuid(),
                Code = $"PL{i}",
                Currency = "EUR",
                IsActive = true
            }).ToList();
            await _db.PriceLists.AddRangeAsync(lists, ct);
            await _db.SaveChangesAsync(ct);
        }

        if (await _db.Prices.AnyAsync(ct)) return;

        var rnd = new Random(101);
        var listsIds = await _db.PriceLists.Select(x => x.PriceListId).ToListAsync(ct);
        var variants = await _db.Variants.Select(x => x.VariantId).ToListAsync(ct);

        // √енерим 2 ценовых периода на вариант
        var prices = new List<Entities.Pricing.Price>(variants.Count * listsIds.Count * 2);
        foreach (var v in variants)
            foreach (var pl in listsIds)
            {
                var start1 = DateTimeOffset.UtcNow.AddMonths(-6).AddDays(-rnd.Next(0, 60));
                var start2 = start1.AddMonths(3);
                var end1 = start2;

                var baseAmt = rnd.Next(1000, 25000) / 100m; // 10.00Ц250.00
                prices.Add(new Entities.Pricing.Price
                {
                    PriceId = Guid.NewGuid(),
                    VariantId = v,
                    PriceListId = pl,
                    Amount = baseAmt,
                    ValidFrom = start1,
                    ValidTo = end1
                });
                prices.Add(new Entities.Pricing.Price
                {
                    PriceId = Guid.NewGuid(),
                    VariantId = v,
                    PriceListId = pl,
                    Amount = baseAmt * (decimal)(0.9 + rnd.NextDouble() * 0.3),
                    ValidFrom = start2,
                    ValidTo = null
                });
            }
        await _db.Prices.AddRangeAsync(prices, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureUsersAndCustomersAsync(int customers, CancellationToken ct)
    {
        if (await _db.Customers.AnyAsync(ct)) return;

        var rnd = new Random(555);
        var users = new List<Entities.Auth.User>(customers);
        var custs = new List<Entities.Crm.Customer>(customers);
        for (int i = 1; i <= customers; i++)
        {
            var uid = Guid.NewGuid();
            users.Add(new Entities.Auth.User
            {
                UserId = uid,
                PhoneE164 = $"+43{i:000000000}",
                PasswordHash = "hashed:placeholder",
                IsEmailVerified = rnd.NextDouble() < 0.7,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-rnd.Next(10, 400))
            });
            custs.Add(new Entities.Crm.Customer
            {
                CustomerId = Guid.NewGuid(),
                UserId = uid,
                FirstName = $"User{i}",
                LastName = $"Test{i}",
                Gender = Entities.Crm.Gender.U,
                BirthDate = null,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-rnd.Next(1, 365))
            });
        }

        await _db.Users.AddRangeAsync(users, ct);
        await _db.Customers.AddRangeAsync(custs, ct);
        await _db.SaveChangesAsync(ct);

        // јдреса (по одному дефолтному)
        var addresses = custs.Select(c => new Entities.Crm.Address
        {
            AddressId = Guid.NewGuid(),
            CustomerId = c.CustomerId,
            CountryCode = "AT",
            Region = "Vienna",
            City = "Vienna",
            Street = $"Main st {Random.Shared.Next(1, 200)}",
            Zip = "1010",
            IsDefault = true
        }).ToList();

        await _db.Addresses.AddRangeAsync(addresses, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task GenerateCartsAndItemsAsync(
    int targetCartItems, int maxItemsPerCart, CancellationToken ct)
    {
        var rnd = new Random(777);
        var customers = await _db.Customers.Select(x => x.CustomerId).ToListAsync(ct);
        var variants = await _db.Variants.Select(x => x.VariantId).ToListAsync(ct);
        var priceListCode = await _db.PriceLists.Select(x => x.Code).FirstAsync(ct); // "PL1"

        // 1) создаЄм корзины
        var targetCarts = Math.Max(targetCartItems / 12, 250_000);
        var cartRows = new List<Entities.Sales.Cart>(targetCarts + 1000);

        for (int i = 0; i < targetCarts; i++)
        {
            cartRows.Add(new Entities.Sales.Cart
            {
                CartId = Guid.NewGuid(),
                CustomerId = customers[rnd.Next(customers.Count)],
                Currency = "EUR",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-rnd.Next(0, 120)),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await BulkInserter.CopyCartsAsync(_conn, cartRows, ct);

        // 2) позиции корзин Ч льЄм батчами в staging и затем UPSERT
        var cartIds = cartRows.Select(x => x.CartId).ToArray();
        int remaining = targetCartItems;

        while (remaining > 0)
        {
            var take = Math.Min(remaining, 100_000); // размер батча
            var batch = new List<Entities.Sales.CartItem>(take);

            for (int i = 0; i < take; i++)
            {
                var cartId = cartIds[rnd.Next(cartIds.Length)];
                var variantId = variants[rnd.Next(variants.Count)];
                var qty = rnd.Next(1, 4);

                batch.Add(new Entities.Sales.CartItem
                {
                    CartItemId = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Qty = qty,
                    PriceSnapshot = 0m // проставим массово после
                });
            }

            await BulkInserter.CopyCartItemsToStagingAndUpsertAsync(_conn, batch, ct);
            remaining -= take;
        }

        // 3) ћассово проставим price_snapshot из активных цен (одним апдейтом)
        const string sql = @"
UPDATE sales.cart_items ci
SET price_snapshot = sub.amount
FROM (
  SELECT v.variant_id, v.amount
  FROM pricing.prices v
) sub
WHERE ci.variant_id = sub.variant_id AND ci.price_snapshot = 0";
        await _db.Database.ExecuteSqlRawAsync(sql, ct);
    }

}
