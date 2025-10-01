using ECommerce.Data.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Pricing;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> b)
    {
        b.ToTable("price_lists", "pricing");
        b.HasKey(x => x.PriceListId).HasName("price_lists_pkey");
        b.Property(x => x.PriceListId).HasColumnName("price_list_id");

        b.Property(x => x.Code).HasColumnName("code").IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("price_lists_code_key");
    }
}
