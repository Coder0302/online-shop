namespace ECommerce.Data.Entities.Pricing;

public class Price
{
    public Guid PriceId { get; set; }
    public Guid VariantId { get; set; }
    public Guid PriceListId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }

    public Catalog.Variant Variant { get; set; } = null!;
    public PriceList PriceList { get; set; } = null!;
}
