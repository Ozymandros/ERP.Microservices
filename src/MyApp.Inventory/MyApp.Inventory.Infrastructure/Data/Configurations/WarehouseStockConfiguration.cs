using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Infrastructure.Data.Configurations;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.AvailableQuantity).IsRequired();
        builder.Property(x => x.ReservedQuantity).IsRequired();
        builder.Property(x => x.OnOrderQuantity).IsRequired();

        // Relationships
        builder.HasOne(x => x.Product)
            .WithMany(p => p.WarehouseStocks)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
    }
}
