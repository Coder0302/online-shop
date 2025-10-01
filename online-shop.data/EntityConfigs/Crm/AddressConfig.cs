using ECommerce.Data.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Crm;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.ToTable("addresses", "crm");
        b.HasKey(x => x.AddressId).HasName("addresses_pkey");
        b.Property(x => x.AddressId).HasColumnName("address_id");

        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)");
        b.Property(x => x.Region).HasColumnName("region");
        b.Property(x => x.City).HasColumnName("city").IsRequired();
        b.Property(x => x.Street).HasColumnName("street").IsRequired();
        b.Property(x => x.Zip).HasColumnName("zip");
        b.Property(x => x.IsDefault).HasColumnName("is_default");

        b.HasOne(x => x.Customer)
         .WithMany(c => c.Addresses)
         .HasForeignKey(x => x.CustomerId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("addresses_customer_id_fkey");

        b.HasIndex(x => x.CustomerId).HasDatabaseName("idx_addresses_customer");
        b.HasIndex(x => new { x.CustomerId })
         .HasFilter("is_default")
         .IsUnique()
         .HasDatabaseName("ux_addresses_default_one");
    }
}
