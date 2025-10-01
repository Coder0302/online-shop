namespace ECommerce.Data.Seeding;
public record SeedOptions(
    int Brands = 1000,
    int Categories = 100,
    int Products = 5000,
    int VariantsPerProductMin = 1,
    int VariantsPerProductMax = 3,
    int Warehouses = 5,
    int PriceLists = 2,
    int Customers = 15000,
    int TargetCartItems = 3_200_000,
    int MaxItemsPerCart = 12
);
