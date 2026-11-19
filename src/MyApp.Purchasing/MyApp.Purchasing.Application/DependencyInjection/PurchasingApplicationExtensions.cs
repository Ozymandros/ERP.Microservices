using Microsoft.Extensions.DependencyInjection;
using MyApp.Purchasing.Application.Internals;
using MyApp.Purchasing.Application.UseCases.PurchaseOrders;

namespace MyApp.Purchasing.Application.DependencyInjection;

public static class PurchasingApplicationExtensions
{
    public static IServiceCollection AddPurchasingApplication(this IServiceCollection services)
    {
        services.AddScoped<IPurchasingTransactionBoundary, PurchasingTransactionBoundary>();
        services.AddScoped<ReceivePurchaseOrderUseCase>();
        services.AddScoped<MatchPurchaseOrderUseCase>();
        return services;
    }
}