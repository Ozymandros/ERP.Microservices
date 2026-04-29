using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Provides Reserved Stock Configuration functionality.
    /// </summary>
    public class ReservedStockConfiguration : IEntityTypeConfiguration<ReservedStock>
    {
        /// <summary>Configure.</summary>
        public void Configure(EntityTypeBuilder<ReservedStock> builder)
        {
            builder.ToTable("ReservedStocks");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.WarehouseId).IsRequired();
            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.ReservedUntil).IsRequired();
            builder.Property(x => x.Status).IsRequired();

            // Indexes
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReservedUntil);
        }
    }
}
