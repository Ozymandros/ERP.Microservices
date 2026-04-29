using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="InventoryReservation"/>.
/// </summary>
public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.OrderLineId);  // nullable Guid

        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.ReservedUntil).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // Relationships — no inverse navigation collections on Product / Warehouse
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.Status);
    }
}
