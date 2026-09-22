using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Services;

/// <summary>
/// Provides Account Service functionality.
/// </summary>
public sealed class AccountService : AppServiceBase, IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountService> _logger;

    /// <summary>I Logger.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public AccountService(
        IAccountRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<AccountService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Crm)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<AccountDto>(entity);
    }

    /// <summary>Get By Customer Id Async.</summary>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
public async Task<AccountDto?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        return entity is null ? null : _mapper.Map<AccountDto>(entity);
    }

    /// <summary>Gets an account by its tax identification number.</summary>
    /// Gets the tax id asynchronously.
    /// <param name="taxId">The tax Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    public async Task<AccountDto?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByTaxIdAsync(taxId, cancellationToken);
        return entity is null ? null : _mapper.Map<AccountDto>(entity);
    }

    /// <summary>Gets all accounts.</summary>
    /// Lists items asynchronously.
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A collection of all account DTOs.</returns>
    public async Task<IEnumerable<AccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<AccountDto>>(list);
    }

    /// <summary>Query Async.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<PaginatedResult<AccountDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default)
    {
        var spec = new AccountQuerySpec(query);
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(a => _mapper.Map<AccountDto>(a)).ToList();
        return new PaginatedResult<AccountDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Upsert From Sales Async.</summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<AccountDto> UpsertFromSalesAsync(UpsertAccountDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.CustomerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(dto.CustomerId));

        var entity = await _repository.GetByCustomerIdAsync(dto.CustomerId, cancellationToken);
        if (entity is null)
        {
            entity = new Account(Guid.NewGuid(), dto.CustomerId, dto.Name);
            entity.UpsertFromSalesSnapshot(dto.Name, dto.TaxId, dto.BillingAddress, dto.ShippingAddress, dto.SyncedAt);
            await _repository.AddAsync(entity);
        }
        else
        {
            entity.UpsertFromSalesSnapshot(dto.Name, dto.TaxId, dto.BillingAddress, dto.ShippingAddress, dto.SyncedAt);
            await _repository.UpdateAsync(entity);
        }

        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Upserted CRM account snapshot for CustomerId={CustomerId}", dto.CustomerId);
        return _mapper.Map<AccountDto>(entity);
    }

    /// <summary>Update Owner Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<AccountDto> UpdateOwnerAsync(Guid id, UpdateAccountOwnerDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Account with ID {id} not found.");

        entity.SetOwner(dto.OwnerUsername);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        return _mapper.Map<AccountDto>(entity);
    }
}

