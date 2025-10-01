using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.EntityConfig.Catalog;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories", "catalog");
        b.HasKey(x => x.CategoryId).HasName("categories_pkey");
        b.Property(x => x.CategoryId).HasColumnName("category_id");

        b.Property(x => x.ParentId).HasColumnName("parent_id");
        b.Property(x => x.Slug).HasColumnName("slug").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").IsRequired();

        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("categories_slug_key");

        b.HasOne(x => x.Parent)
         .WithMany(x => x.Children)
         .HasForeignKey(x => x.ParentId)
         .HasConstraintName("categories_parent_id_fkey");
    }
}
