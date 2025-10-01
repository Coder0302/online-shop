using ECommerce.Data.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Crm;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers", "crm");
        b.HasKey(x => x.CustomerId).HasName("customers_pkey");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");

        b.Property(x => x.UserId).HasColumnName("user_id");
        b.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("customers_user_id_key");

        b.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
        b.Property(x => x.LastName).HasColumnName("last_name").IsRequired();

        b.Property(x => x.Gender)
         .HasConversion<string>()
         .HasColumnName("gender")
         .IsRequired();

        b.Property(x => x.BirthDate).HasColumnName("birth_date");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasOne(x => x.User)
         .WithOne(u => u.Customer!)
         .HasForeignKey<Customer>(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade)
         .HasConstraintName("customers_user_id_fkey");
    }
}
