using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace ECommerce.Data;
public class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{

    // DbSet'ы по таблицам
    public DbSet<Entities.Auth.User> Users => Set<Entities.Auth.User>();
    public DbSet<Entities.Catalog.Brand> Brands => Set<Entities.Catalog.Brand>();
    public DbSet<Entities.Catalog.Category> Categories => Set<Entities.Catalog.Category>();
    public DbSet<Entities.Catalog.Product> Products => Set<Entities.Catalog.Product>();
    public DbSet<Entities.Catalog.Variant> Variants => Set<Entities.Catalog.Variant>();
    public DbSet<Entities.Catalog.ProductMedia> ProductMedia => Set<Entities.Catalog.ProductMedia>();
    public DbSet<Entities.Crm.Customer> Customers => Set<Entities.Crm.Customer>();
    public DbSet<Entities.Crm.Address> Addresses => Set<Entities.Crm.Address>();
    public DbSet<Entities.Inventory.Warehouse> Warehouses => Set<Entities.Inventory.Warehouse>();
    public DbSet<Entities.Inventory.StockItem> StockItems => Set<Entities.Inventory.StockItem>();
    public DbSet<Entities.Pricing.PriceList> PriceLists => Set<Entities.Pricing.PriceList>();
    public DbSet<Entities.Pricing.Price> Prices => Set<Entities.Pricing.Price>();
    public DbSet<Entities.Sales.Cart> Carts => Set<Entities.Sales.Cart>();
    public DbSet<Entities.Sales.CartItem> CartItems => Set<Entities.Sales.CartItem>();



    public DbSet<PriceRankWithinCategoryDto> PriceRankWithinCategory => Set<PriceRankWithinCategoryDto>();
    public DbSet<LatestPriceDto> LatestPrices => Set<LatestPriceDto>();
    public DbSet<RunningCartValueDto> RunningCartValues => Set<RunningCartValueDto>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("pgcrypto");
        b.HasPostgresExtension("btree_gist");

        b.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly); // подхватит все *Config.cs

        b.Entity<PriceRankWithinCategoryDto>().HasNoKey().ToView(null);
        b.Entity<LatestPriceDto>().HasNoKey().ToView(null);
        b.Entity<RunningCartValueDto>().HasNoKey().ToView(null);
    }
}

[Keyless]
public sealed class PriceRankWithinCategoryDto
{
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public Guid VariantId { get; set; }
    public decimal Price { get; set; }
    public int PriceRankInCategory { get; set; }
}

[Keyless]
public sealed class LatestPriceDto
{
    public Guid VariantId { get; set; }
    public string PriceListCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}

[Keyless]
public sealed class RunningCartValueDto
{
    public Guid? CustomerId { get; set; }
    public Guid CartId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public decimal Total { get; set; }
    public decimal RunningTotalByCustomer { get; set; }
}