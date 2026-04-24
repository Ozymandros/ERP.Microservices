using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Application.Services;
using MyApp.Billing.Domain.Repositories;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Billing.API",
    ConnectionStringKey = "billingdb",
    DbContextType = typeof(BillingDbContext),
    AutoMapperAssembly = typeof(BillingMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        // Register repositories
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();

        // Register application services
        services.AddScoped<IInvoiceService, InvoiceService>();
    }
});

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
