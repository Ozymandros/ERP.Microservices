using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Infrastructure.Data.Configurations
{
    public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
    {
        public void Configure(EntityTypeBuilder<SalesOrder> builder)
        {
            builder.ToTable("SalesOrders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.OrderDate).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
