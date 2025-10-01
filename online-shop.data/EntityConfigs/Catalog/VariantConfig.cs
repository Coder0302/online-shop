using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Catalog;

public class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> b)
    {
        b.ToTable("variants", "catalog");
        b.HasKey(x => x.VariantId).HasName("variants_pkey");
        b.Property(x => x.VariantId).HasColumnName("variant_id");

        b.Property(x => x.ProductId).HasColumnName("product_id");
        b.Property(x => x.Sku).HasColumnName("sku").IsRequired();
        b.HasIndex(x => x.Sku).IsUnique().HasDatabaseName("variants_sku_key");

        b.Property(x => x.OptionKvJson)
         .HasColumnName("option_kv")
         .HasColumnType("jsonb")
         .HasDefaultValue("{}");

        b.Property(x => x.Barcode).HasColumnName("barcode");
        b.Property(x => x.WeightG).HasColumnName("weight_g");
        b.Property(x => x.DimensionsMm)
         .HasColumnName("dimensions_mm")
         .HasColumnType("integer[]");

        b.HasOne(x => x.Product)
         .WithMany(x => x.Variants)
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("variants_product_id_fkey");

        b.HasIndex("OptionKvJson").HasDatabaseName("idx_variants_optionkv_gin").HasMethod("gin");
    }
}
