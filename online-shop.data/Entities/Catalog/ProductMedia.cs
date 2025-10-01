namespace ECommerce.Data.Entities.Catalog;

public enum MediaKind { Image, Video, Manual }

public class ProductMedia
{
    public Guid MediaId { get; set; }
    public Guid ProductId { get; set; }
    public string Url { get; set; } = null!;
    public MediaKind Kind { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
