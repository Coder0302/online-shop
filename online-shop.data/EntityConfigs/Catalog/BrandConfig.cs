using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Catalog;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.ToTable("brands", "catalog");
        b.HasKey(x => x.BrandId).HasName("brands_pkey");
        b.Property(x => x.BrandId).HasColumnName("brand_id");
        b.Property(x => x.Name).HasColumnName("name").IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("brands_name_key");
    }
}
