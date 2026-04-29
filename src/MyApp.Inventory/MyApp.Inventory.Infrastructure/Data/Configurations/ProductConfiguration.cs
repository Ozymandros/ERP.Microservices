using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Product Configuration functionality.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.QuantityInStock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReorderLevel)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(x => x.SKU)
            .IsUnique();

        builder.HasMany(x => x.InventoryTransactions)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
