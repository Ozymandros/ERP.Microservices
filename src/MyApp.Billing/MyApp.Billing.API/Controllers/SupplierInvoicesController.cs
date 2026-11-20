using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Billing.API.Controllers;

/// <summary>Supplier (AP) invoices for 3-way match.</summary>
[Route("api/billing/supplier-invoices")]
[Authorize]
[ApiController]
public class SupplierInvoicesController : ControllerBase
{
    private readonly ISupplierInvoiceService _service;
    private readonly ICacheService _cacheService;

    public SupplierInvoicesController(ISupplierInvoiceService service, ICacheService cacheService)
    {
        _service = service;
        _cacheService = cacheService;
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Billing", "Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _cacheService.GetStateAsync<SupplierInvoiceDto>($"supplier_invoice_{id}")
            ?? await _service.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            return NotFound();
        await _cacheService.SaveStateAsync($"supplier_invoice_{id}", invoice);
        return Ok(invoice);
    }

    [HttpGet("by-purchase-order/{purchaseOrderId:guid}")]
    [HasPermission("Billing", "Read")]
    public async Task<IActionResult> GetByPurchaseOrder(Guid purchaseOrderId, CancellationToken cancellationToken)
    {
        var cacheKey = $"supplier_invoices_po_{purchaseOrderId}";
        var invoices = await _cacheService.GetStateAsync<List<SupplierInvoiceDto>>(cacheKey);
        if (invoices is null)
        {
            invoices = await _service.GetByPurchaseOrderIdAsync(purchaseOrderId, cancellationToken);
            // Do not cache null/empty — avoids poisoning and lets later creates become visible without relying only on invalidation.
            if (invoices is { Count: > 0 })
                await _cacheService.SaveStateAsync(cacheKey, invoices);
        }

        return Ok(invoices);
    }

    [HttpPost]
    [HasPermission("Billing", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierInvoiceDto dto, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(dto, cancellationToken);
        await _cacheService.RemoveStateAsync($"supplier_invoices_po_{dto.PurchaseOrderId}");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
