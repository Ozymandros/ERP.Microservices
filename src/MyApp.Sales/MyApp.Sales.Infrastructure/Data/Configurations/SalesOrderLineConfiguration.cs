using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Provides Sales Order Line Configuration functionality.
    /// </summary>
    public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
    {
        /// <summary>Configure.</summary>
        public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
        {
            builder.ToTable("SalesOrderLines");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        }
    }
}
