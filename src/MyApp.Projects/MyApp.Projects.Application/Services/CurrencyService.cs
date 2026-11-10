using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Projects.Application.Services;

/// <summary>Currency and exchange rate management (local currency from <see cref="ProjectsSettings"/>).</summary>
public class CurrencyService : AppServiceBase, ICurrencyService
{
    private readonly ICurrencyRepository _currencies;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="CurrencyService"/> class.</summary>
    public CurrencyService(
        ICurrencyRepository currencies,
        IMapper mapper,
        ProjectsSettings settings,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<CurrencyService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _currencies = currencies;
        _mapper = mapper;
        LocalCurrency = settings.LocalCurrency.ToUpperInvariant();
    }

    /// <inheritdoc />
    public string LocalCurrency { get; }

    /// <inheritdoc />
    public async Task<List<CurrencyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var currencies = await _currencies.ListAsync(cancellationToken);
        return currencies.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var currency = await _currencies.GetByCodeAsync(code, cancellationToken);
        return currency is null ? null : ToDto(currency);
    }

    /// <inheritdoc />
    public async Task<CurrencyDto> CreateAsync(CreateUpdateCurrencyDto dto, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(dto.Code);
        if (await _currencies.GetByCodeAsync(code, cancellationToken) is not null)
            throw new InvalidOperationException($"Currency {code} already exists.");

        var currency = new Currency(Guid.NewGuid())
        {
            Code = code,
            Description = dto.Description.Trim(),
            Symbol = dto.Symbol
        };

        await _currencies.AddAsync(currency);
        await SaveChangesAsync(cancellationToken);
        return ToDto(currency);
    }

    /// <inheritdoc />
    public async Task<CurrencyDto> UpdateAsync(string code, CreateUpdateCurrencyDto dto, CancellationToken cancellationToken = default)
    {
        var currency = await _currencies.GetByCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), code);

        if (!string.Equals(NormalizeCode(dto.Code), currency.Code, StringComparison.Ordinal))
            throw new ArgumentException("The currency code cannot be changed.");

        currency.Description = dto.Description.Trim();
        currency.Symbol = dto.Symbol;
        await SaveChangesAsync(cancellationToken);
        return ToDto(currency);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string code, CancellationToken cancellationToken = default)
    {
        var currency = await _currencies.GetByCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), code);

        if (currency.Code == LocalCurrency)
            throw new InvalidOperationException("The local currency cannot be deleted.");
        if (await _currencies.IsInUseAsync(currency.Code, cancellationToken))
            throw new InvalidOperationException($"Currency {currency.Code} is used by one or more projects.");

        await _currencies.DeleteAsync(currency);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExchangeRateDto> AddExchangeRateAsync(string code, CreateExchangeRateDto dto, CancellationToken cancellationToken = default)
    {
        var currency = await _currencies.GetByCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), code);

        if (currency.Code == LocalCurrency)
            throw new InvalidOperationException("The local currency does not have exchange rates.");
        if (dto.RateToLocal <= 0)
            throw new ArgumentException("RateToLocal must be greater than zero.");

        var startingDate = dto.StartingDate.Date;
        if (currency.ExchangeRates.Any(r => r.StartingDate.Date == startingDate))
            throw new InvalidOperationException($"An exchange rate for {currency.Code} starting {startingDate:yyyy-MM-dd} already exists.");

        var rate = new ExchangeRate(Guid.NewGuid())
        {
            CurrencyId = currency.Id,
            StartingDate = startingDate,
            RateToLocal = dto.RateToLocal
        };

        await _currencies.AddExchangeRateAsync(rate);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ExchangeRateDto>(rate);
    }

    /// <inheritdoc />
    public async Task DeleteExchangeRateAsync(string code, Guid rateId, CancellationToken cancellationToken = default)
    {
        var currency = await _currencies.GetByCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), code);
        var rate = await _currencies.GetExchangeRateAsync(rateId, cancellationToken);
        if (rate is null || rate.CurrencyId != currency.Id)
            throw new NotFoundException(nameof(ExchangeRate), rateId);

        _currencies.RemoveExchangeRate(rate);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetRateAsync(string currencyCode, DateTime date, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(currencyCode);
        if (code == LocalCurrency)
            return 1m;

        return await _currencies.GetRateAsync(code, date, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No exchange rate for {code} valid on {date:yyyy-MM-dd}. Add one under /api/projects/currencies/{code}/exchange-rates.");
    }

    private CurrencyDto ToDto(Currency currency) =>
        _mapper.Map<CurrencyDto>(currency) with { IsLocalCurrency = currency.Code == LocalCurrency };

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.");
        return code.Trim().ToUpperInvariant();
    }
}
