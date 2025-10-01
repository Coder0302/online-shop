namespace ECommerce.Data.Entities.Sales;

public class Cart
{
    public Guid CartId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Currency { get; set; } = null!; // char(3)
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Crm.Customer? Customer { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
