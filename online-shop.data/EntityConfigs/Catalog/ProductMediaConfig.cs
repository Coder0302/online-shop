using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Catalog;

public class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> b)
    {
        b.ToTable("product_media", "catalog");
        b.HasKey(x => x.MediaId).HasName("product_media_pkey");
        b.Property(x => x.MediaId).HasColumnName("media_id");

        b.Property(x => x.ProductId).HasColumnName("product_id");
        b.Property(x => x.Url).HasColumnName("url").IsRequired();
        b.Property(x => x.SortOrder).HasColumnName("sort_order");

        b.Property(x => x.Kind)
         .HasConversion<string>()
         .HasColumnName("kind")
         .IsRequired();

        b.HasOne(x => x.Product)
         .WithMany(x => x.Media)
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("product_media_product_id_fkey");

        b.HasIndex(x => new { x.ProductId, x.Kind, x.SortOrder })
         .HasDatabaseName("idx_media_product_kind_sort");
    }
}
