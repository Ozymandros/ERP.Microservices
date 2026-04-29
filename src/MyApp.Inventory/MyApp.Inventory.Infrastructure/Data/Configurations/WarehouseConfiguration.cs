using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Warehouse Configuration functionality.
/// </summary>
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Location)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasMany(x => x.InventoryTransactions)
            .WithOne(x => x.Warehouse)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
