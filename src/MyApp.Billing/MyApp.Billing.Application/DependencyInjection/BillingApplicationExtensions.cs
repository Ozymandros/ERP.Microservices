using Microsoft.Extensions.DependencyInjection;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.UseCases.Invoices;
using MyApp.Billing.Application.UseCases.SupplierInvoices;

namespace MyApp.Billing.Application.DependencyInjection;

/// <summary>Registers Billing application use cases and infrastructure.</summary>
public static class BillingApplicationExtensions
{
    /// <summary>Adds Billing hexagonal application services.</summary>
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        services.AddScoped<IBillingTransactionBoundary, BillingTransactionBoundary>();

        services.AddScoped<CreateInvoiceUseCase>();
        services.AddScoped<IssueInvoiceUseCase>();
        services.AddScoped<RecordPaymentUseCase>();
        services.AddScoped<CancelInvoiceUseCase>();
        services.AddScoped<CreateCreditNoteUseCase>();
        services.AddScoped<CreateFromProjectRequestUseCase>();
        services.AddScoped<CreateAndIssueFromFulfillmentUseCase>();
        services.AddScoped<CreateCreditNoteFromReturnFulfillmentUseCase>();
        services.AddScoped<CreateSupplierInvoiceUseCase>();

        return services;
    }
}