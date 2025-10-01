using System.ComponentModel;

namespace ECommerce.Data.Entities.Catalog;

public enum ProductStatus
{
    [Description("draft")]
    Draft,

    [Description("active")]
    Active,

    [Description("archived")]
    Archived
}

public class Product
{
    public Guid ProductId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public string SkuBase { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public string AttrsJson { get; set; } = "{}"; // jsonb
    public DateTimeOffset CreatedAt { get; set; }

    public Brand? Brand { get; set; }
    public Category? Category { get; set; }
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();
}
