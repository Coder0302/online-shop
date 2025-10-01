namespace ECommerce.Data.Entities.Inventory;

public class StockItem
{
    public Guid WarehouseId { get; set; }
    public Guid VariantId { get; set; }
    public int QtyOnHand { get; set; }
    public int QtyReserved { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Catalog.Variant Variant { get; set; } = null!;
}
