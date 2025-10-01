namespace ECommerce.Data.Entities.Catalog;

public class Variant
{
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string OptionKvJson { get; set; } = "{}"; // jsonb
    public string? Barcode { get; set; }
    public int? WeightG { get; set; }
    public int[]? DimensionsMm { get; set; } // length=3

    public Product Product { get; set; } = null!;
    public ICollection<Inventory.StockItem> StockItems { get; set; } = new List<Inventory.StockItem>();
    public ICollection<Pricing.Price> Prices { get; set; } = new List<Pricing.Price>();
    public ICollection<Sales.CartItem> CartItems { get; set; } = new List<Sales.CartItem>();
}
