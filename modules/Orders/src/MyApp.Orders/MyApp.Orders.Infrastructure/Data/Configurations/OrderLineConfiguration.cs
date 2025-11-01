using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Infrastructure.Data.Configurations
{
    public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> builder)
        {
            builder.ToTable("OrderLines");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        }
    }
}
