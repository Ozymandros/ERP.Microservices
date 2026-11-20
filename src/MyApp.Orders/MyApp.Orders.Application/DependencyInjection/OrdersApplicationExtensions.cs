using Microsoft.Extensions.DependencyInjection;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Application.UseCases.Orders;

namespace MyApp.Orders.Application.DependencyInjection;

/// <summary>Registers Orders application use cases.</summary>
public static class OrdersApplicationExtensions
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrdersTransactionBoundary, OrdersTransactionBoundary>();
        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<UpdateOrderUseCase>();
        services.AddScoped<DeleteOrderUseCase>();
        services.AddScoped<CancelOrderUseCase>();
        services.AddScoped<FulfillOrderUseCase>();
        services.AddScoped<CreateOrderWithReservationUseCase>();
        return services;
    }
}