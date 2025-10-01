using ECommerce.Data.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Auth;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users", "auth");
        b.HasKey(x => x.UserId).HasName("users_pkey");
        b.Property(x => x.UserId).HasColumnName("user_id");

        b.Property(x => x.PhoneE164).HasColumnName("phone_e164");
        b.HasIndex(x => x.PhoneE164).IsUnique().HasDatabaseName("users_phone_e164_key");

        b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        b.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasOne(x => x.Customer)
         .WithOne(c => c.User!)
         .HasForeignKey<ECommerce.Data.Entities.Crm.Customer>(c => c.UserId)
         .HasConstraintName("customers_user_id_fkey");
    }
}
