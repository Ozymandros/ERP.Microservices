using MyApp.Shared.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Domain.Repositories;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Application.Services;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Shared.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Defaults Configuration
// ============================================================================
builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Billing.API",
    EnableHealthChecks = true,
    EnableRedisCache = false,
    EnableAutoMapper = false,
    DbContextType = typeof(BillingDbContext),
    ConnectionStringKey = "BillingDb",
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

// ============================================================================
// Service Defaults Pipeline
// ============================================================================
app.UseServiceDefaults();

// ============================================================================
// Billing API Endpoints
// ============================================================================
app.MapPost("/api/billing/invoices", async (
    MyApp.Billing.Application.Contracts.DTOs.CreateInvoiceDto dto,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.CreateInvoiceAsync(dto, ct);
    return Results.Created($"/api/billing/invoices/{result.Id}", result);
})
.RequireAuthorization()
.WithName("CreateInvoice")
.WithOpenApi();

app.MapGet("/api/billing/invoices/{invoiceId:guid}", async (
    Guid invoiceId,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.GetInvoiceByIdAsync(invoiceId, ct);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetInvoiceById")
.WithOpenApi();

app.MapGet("/api/billing/customers/{customerId:guid}/invoices", async (
    Guid customerId,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.GetInvoicesByCustomerIdAsync(customerId, ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetInvoicesByCustomerId")
.WithOpenApi();

app.MapGet("/api/billing/invoices/open", async (
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.GetOpenInvoicesAsync(ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetOpenInvoices")
.WithOpenApi();

app.MapGet("/api/billing/orders/{orderId:guid}/invoices", async (
    Guid orderId,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.GetInvoicesByOrderIdAsync(orderId, ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetInvoicesByOrderId")
.WithOpenApi();

app.MapPost("/api/billing/invoices/{invoiceId:guid}/issue", async (
    Guid invoiceId,
    IssueInvoiceRequest request,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.IssueInvoiceAsync(invoiceId, request.InvoiceNumber, request.IssueDate, ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("IssueInvoice")
.WithOpenApi();

app.MapPost("/api/billing/invoices/{invoiceId:guid}/payments", async (
    MyApp.Billing.Application.Contracts.DTOs.RecordPaymentDto dto,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.RecordPaymentAsync(dto, ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("RecordPayment")
.WithOpenApi();

app.MapPost("/api/billing/invoices/{invoiceId:guid}/cancel", async (
    Guid invoiceId,
    CancelInvoiceRequest request,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.CancelInvoiceAsync(invoiceId, request.Reason, ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("CancelInvoice")
.WithOpenApi();

app.MapPost("/api/billing/credit-notes", async (
    MyApp.Billing.Application.Contracts.DTOs.CreateCreditNoteDto dto,
    IInvoiceService invoiceService,
    CancellationToken ct) =>
{
    var result = await invoiceService.CreateCreditNoteAsync(dto, ct);
    return Results.Created($"/api/billing/credit-notes/{result.Id}", result);
})
.RequireAuthorization()
.WithName("CreateCreditNote")
.WithOpenApi();

app.Run();

// ============================================================================
// Request DTOs for endpoints that don't use the main DTOs
// ============================================================================
public record IssueInvoiceRequest(string InvoiceNumber, DateTime IssueDate);
public record CancelInvoiceRequest(string Reason);
