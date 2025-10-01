using ECommerce.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Sales;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.ToTable("carts", "sales");
        b.HasKey(x => x.CartId).HasName("carts_pkey");
        b.Property(x => x.CartId).HasColumnName("cart_id");

        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(x => x.Customer)
         .WithMany(c => c.Carts)
         .HasForeignKey(x => x.CustomerId)
         .HasConstraintName("carts_customer_id_fkey");

        b.HasIndex(x => x.CustomerId).HasDatabaseName("idx_carts_customer");
    }
}
