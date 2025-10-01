namespace ECommerce.Data.Entities.Sales;

public class CartItem
{
    public Guid CartItemId { get; set; }
    public Guid CartId { get; set; }
    public Guid VariantId { get; set; }
    public int Qty { get; set; }
    public decimal PriceSnapshot { get; set; }

    public Cart Cart { get; set; } = null!;
    public Catalog.Variant Variant { get; set; } = null!;
}
