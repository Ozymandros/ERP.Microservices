using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Projects.API.Controllers;

/// <summary>Currencies and exchange rates used to price projects (rates are LCY per 1 unit of currency).</summary>
[ApiController]
[Authorize]
[Route("api/projects/currencies")]
[Produces("application/json")]
public class CurrenciesController : ProjectsApiControllerBase
{
    private readonly ICurrencyService _currencyService;

    /// <summary>Initializes a new instance of the <see cref="CurrenciesController"/> class.</summary>
    public CurrenciesController(ICurrencyService currencyService, ICacheService cacheService, ILogger<CurrenciesController> logger)
        : base(cacheService, logger)
    {
        _currencyService = currencyService;
    }

    /// <summary>Export all currencies as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportToXlsx() => ExecuteAsync(async () =>
    {
        var currencies = await _currencyService.ListAsync(HttpContext.RequestAborted);
        return File(currencies.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Currencies.xlsx");
    }, "exporting currencies");

    /// <summary>Export all currencies as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportToPdf() => ExecuteAsync(async () =>
    {
        var currencies = await _currencyService.ListAsync(HttpContext.RequestAborted);
        return File(currencies.ExportToPdf(), "application/pdf", "Currencies.pdf");
    }, "exporting currencies");

    /// <summary>List currencies with their exchange rates.</summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<CurrencyDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() => ExecuteAsync(
        async () => Ok(await _currencyService.ListAsync(HttpContext.RequestAborted)),
        "retrieving currencies");

    /// <summary>Get a currency by ISO code.</summary>
    [HttpGet("{code}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCode(string code) => ExecuteAsync(async () =>
    {
        var currency = await _currencyService.GetByCodeAsync(code, HttpContext.RequestAborted);
        return currency is null ? NotFound(new { message = $"Currency {code} not found." }) : Ok(currency);
    }, "retrieving the currency");

    /// <summary>Get the exchange rate valid on a date (defaults to today).</summary>
    [HttpGet("{code}/rate")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public Task<IActionResult> GetRate(string code, [FromQuery] DateTime? date) => ExecuteAsync(
        async () => Ok(new
        {
            currencyCode = code.ToUpperInvariant(),
            localCurrency = _currencyService.LocalCurrency,
            date = (date ?? DateTime.UtcNow).Date,
            rateToLocal = await _currencyService.GetRateAsync(code, date ?? DateTime.UtcNow, HttpContext.RequestAborted)
        }),
        "retrieving the exchange rate");

    /// <summary>Create a currency.</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status201Created)]
    public Task<IActionResult> Create([FromBody] CreateUpdateCurrencyDto dto) => ExecuteAsync(async () =>
    {
        var currency = await _currencyService.CreateAsync(dto, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetByCode), new { code = currency.Code }, currency);
    }, "creating the currency");

    /// <summary>Update a currency's description and symbol.</summary>
    [HttpPut("{code}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(string code, [FromBody] CreateUpdateCurrencyDto dto) => ExecuteAsync(
        async () => Ok(await _currencyService.UpdateAsync(code, dto, HttpContext.RequestAborted)),
        "updating the currency");

    /// <summary>Delete a currency (not used by any project).</summary>
    [HttpDelete("{code}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Delete(string code) => ExecuteAsync(async () =>
    {
        await _currencyService.DeleteAsync(code, HttpContext.RequestAborted);
        return NoContent();
    }, "deleting the currency");

    /// <summary>Add an exchange rate (LCY per 1 unit of the currency) valid from a starting date.</summary>
    [HttpPost("{code}/exchange-rates")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    public Task<IActionResult> AddExchangeRate(string code, [FromBody] CreateExchangeRateDto dto) => ExecuteAsync(
        async () => Ok(await _currencyService.AddExchangeRateAsync(code, dto, HttpContext.RequestAborted)),
        "adding the exchange rate");

    /// <summary>Delete an exchange rate.</summary>
    [HttpDelete("{code}/exchange-rates/{rateId:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> DeleteExchangeRate(string code, Guid rateId) => ExecuteAsync(async () =>
    {
        await _currencyService.DeleteExchangeRateAsync(code, rateId, HttpContext.RequestAborted);
        return NoContent();
    }, "deleting the exchange rate");
}
