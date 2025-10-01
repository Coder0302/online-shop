namespace ECommerce.Data.Entities.Catalog;

public class Category
{
    public Guid CategoryId { get; set; }
    public Guid? ParentId { get; set; }
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
