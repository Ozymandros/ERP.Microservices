using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Infrastructure.Data.Configurations
{
    /// <summary>Entity Framework configuration for OrderLine entity.</summary>
    public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        /// <summary>Configures the OrderLine entity mapping.</summary>
        public void Configure(EntityTypeBuilder<OrderLine> builder)
        {
            builder.ToTable("OrderLines");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.PickedQuantity).IsRequired();
            builder.Property(x => x.IsFulfilled).IsRequired();
        }
    }
}
