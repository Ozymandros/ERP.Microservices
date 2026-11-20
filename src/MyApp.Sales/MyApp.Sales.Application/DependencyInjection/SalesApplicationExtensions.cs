using Microsoft.Extensions.DependencyInjection;
using MyApp.Sales.Application.Internals;
using MyApp.Sales.Application.UseCases.SalesOrders;

namespace MyApp.Sales.Application.DependencyInjection;

public static class SalesApplicationExtensions
{
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddScoped<ISalesTransactionBoundary, SalesTransactionBoundary>();
        services.AddScoped<CheckStockAvailabilityUseCase>();
        services.AddScoped<ConfirmQuoteUseCase>();
        services.AddScoped<CreateReturnUseCase>();
        return services;
    }
}