using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Catalog;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products", "catalog");
        b.HasKey(x => x.ProductId).HasName("products_pkey");
        b.Property(x => x.ProductId).HasColumnName("product_id");

        b.Property(x => x.BrandId).HasColumnName("brand_id");
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.Property(x => x.SkuBase).HasColumnName("sku_base").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.Property(x => x.Status)
         .HasConversion<string>()
         .HasColumnName("status")
         .IsRequired();

        // jsonb
        b.Property(x => x.AttrsJson)
         .HasColumnName("attrs")
         .HasColumnType("jsonb")
         .HasDefaultValue("{}");

        b.HasOne(x => x.Brand)
         .WithMany(x => x.Products)
         .HasForeignKey(x => x.BrandId)
         .HasConstraintName("products_brand_id_fkey");

        b.HasOne(x => x.Category)
         .WithMany(x => x.Products)
         .HasForeignKey(x => x.CategoryId)
         .HasConstraintName("products_category_id_fkey");

        // partial index: status = 'active'
        b.HasIndex(x => x.Status).HasDatabaseName("idx_products_active")
         .HasFilter("(status = 'active')");
        // GIN по attrs (создадим через миграцию raw SQL, либо использовать HasMethod)
        b.HasIndex("AttrsJson").HasDatabaseName("idx_products_attrs_gin")
         .HasMethod("gin");

        b.Property(p => p.Status)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<ProductStatus>(v, true));
    }
}
