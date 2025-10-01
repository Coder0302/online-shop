using ECommerce.Data.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Inventory;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses", "inventory");
        b.HasKey(x => x.WarehouseId).HasName("warehouses_pkey");
        b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");

        b.Property(x => x.Code).HasColumnName("code").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").IsRequired();
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("warehouses_code_key");
    }
}
