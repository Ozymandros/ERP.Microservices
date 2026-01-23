using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.OrderDate).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.Type).IsRequired();
            
            // Operational fields
            builder.Property(x => x.SourceId);
            builder.Property(x => x.TargetId);
            builder.Property(x => x.ExternalOrderId);
            
            // Fulfillment fields
            builder.Property(x => x.DestinationAddress).HasMaxLength(500);
            builder.Property(x => x.TrackingNumber).HasMaxLength(100);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
