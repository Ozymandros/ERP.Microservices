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
            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.OrderDate).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
