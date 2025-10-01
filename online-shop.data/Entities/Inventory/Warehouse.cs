namespace ECommerce.Data.Entities.Inventory;

public class Warehouse
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string CountryCode { get; set; } = null!; // char(2)

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
