using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Services;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountRepository repository, IMapper mapper, ILogger<AccountService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<AccountDto>(entity);
    }

    public async Task<AccountDto?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        return entity is null ? null : _mapper.Map<AccountDto>(entity);
    }

    public async Task<IEnumerable<AccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<AccountDto>>(list);
    }

    public async Task<PaginatedResult<AccountDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default)
    {
        var spec = new AccountQuerySpec(query);
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(a => _mapper.Map<AccountDto>(a)).ToList();
        return new PaginatedResult<AccountDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

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

        _logger.LogInformation("Upserted CRM account snapshot for CustomerId={CustomerId}", dto.CustomerId);
        return _mapper.Map<AccountDto>(entity);
    }

    public async Task<AccountDto> UpdateOwnerAsync(Guid id, UpdateAccountOwnerDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Account with ID {id} not found.");

        entity.SetOwner(dto.OwnerUsername);
        await _repository.UpdateAsync(entity);

        return _mapper.Map<AccountDto>(entity);
    }
}

