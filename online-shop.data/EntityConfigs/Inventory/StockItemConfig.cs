using ECommerce.Data.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Inventory;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.ToTable("stock_items", "inventory");
        b.HasKey(x => new { x.WarehouseId, x.VariantId }).HasName("stock_items_pkey");

        b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        b.Property(x => x.VariantId).HasColumnName("variant_id");
        b.Property(x => x.QtyOnHand).HasColumnName("qty_on_hand").HasDefaultValue(0);
        b.Property(x => x.QtyReserved).HasColumnName("qty_reserved").HasDefaultValue(0);

        b.HasOne(x => x.Warehouse)
         .WithMany(w => w.StockItems)
         .HasForeignKey(x => x.WarehouseId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("stock_items_warehouse_id_fkey");

        b.HasOne(x => x.Variant)
         .WithMany(v => v.StockItems)
         .HasForeignKey(x => x.VariantId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("stock_items_variant_id_fkey");

        b.HasIndex(x => x.VariantId).HasDatabaseName("idx_stock_items_variant");
    }
}
