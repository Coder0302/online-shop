using ECommerce.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Sales;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.ToTable("cart_items", "sales");
        b.HasKey(x => x.CartItemId).HasName("cart_items_pkey");
        b.Property(x => x.CartItemId).HasColumnName("cart_item_id");

        b.Property(x => x.CartId).HasColumnName("cart_id");
        b.Property(x => x.VariantId).HasColumnName("variant_id");
        b.Property(x => x.Qty).HasColumnName("qty");
        b.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot").HasColumnType("numeric(12,2)");

        b.HasOne(x => x.Cart)
         .WithMany(c => c.Items)
         .HasForeignKey(x => x.CartId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("cart_items_cart_id_fkey");

        b.HasOne(x => x.Variant)
         .WithMany(v => v.CartItems)
         .HasForeignKey(x => x.VariantId)
         .HasConstraintName("cart_items_variant_id_fkey");

        b.HasIndex(x => new { x.CartId, x.VariantId })
         .IsUnique()
         .HasDatabaseName("cart_items_cart_id_variant_id_key");

        b.HasIndex(x => x.VariantId).HasDatabaseName("idx_cart_items_variant");
    }
}
