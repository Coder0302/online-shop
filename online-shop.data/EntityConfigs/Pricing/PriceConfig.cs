using ECommerce.Data.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Pricing;

public class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> b)
    {
        b.ToTable("prices", "pricing");
        b.HasKey(x => x.PriceId).HasName("prices_pkey");
        b.Property(x => x.PriceId).HasColumnName("price_id");

        b.Property(x => x.VariantId).HasColumnName("variant_id");
        b.Property(x => x.PriceListId).HasColumnName("price_list_id");
        b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
        b.Property(x => x.ValidFrom).HasColumnName("valid_from");
        b.Property(x => x.ValidTo).HasColumnName("valid_to");

        b.HasOne(x => x.Variant)
         .WithMany(v => v.Prices)
         .HasForeignKey(x => x.VariantId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("prices_variant_id_fkey");

        b.HasOne(x => x.PriceList)
         .WithMany(pl => pl.Prices)
         .HasForeignKey(x => x.PriceListId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("prices_price_list_id_fkey");

        b.HasIndex(x => new { x.VariantId, x.PriceListId, x.ValidFrom })
         .IsUnique()
         .HasDatabaseName("prices_variant_id_price_list_id_valid_from_key");

        b.HasIndex(x => new { x.VariantId, x.PriceListId, x.ValidFrom })
         .HasDatabaseName("idx_prices_lookup");
    }
}
