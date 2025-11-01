using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.SupplierId)
            .IsRequired();

        builder.Property(x => x.OrderDate)
            .IsRequired();

        builder.Property(x => x.ExpectedDeliveryDate);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PurchaseOrderStatus.Draft);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.HasIndex(x => x.OrderNumber)
            .IsUnique();

        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OrderDate);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.PurchaseOrder)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
