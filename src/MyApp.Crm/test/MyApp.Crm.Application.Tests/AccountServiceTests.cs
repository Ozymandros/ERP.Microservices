using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Application.Tests;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _sut = new AccountService(
            _repo.Object,
            _mapper.Object,
            _uow.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AccountService>>());
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var account = new Account(id, Guid.NewGuid(), "Acme");
        var dto = new AccountDto(
            id, account.CustomerId, "Acme", null, null, null, true, null,
            account.LastSyncedAt, account.CreatedAt, account.UpdatedAt);

        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _mapper.Setup(m => m.Map<AccountDto>(account)).Returns(dto);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Acme", result!.Name);
    }

    [Fact]
    public async Task UpdateOwnerAsync_Existing_UpdatesOwner()
    {
        var id = Guid.NewGuid();
        var account = new Account(id, Guid.NewGuid(), "Acme");
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _repo.Setup(r => r.UpdateAsync(account)).ReturnsAsync(account);
        _mapper.Setup(m => m.Map<AccountDto>(account))
            .Returns((Account a) => Map(a));

        var result = await _sut.UpdateOwnerAsync(id, new UpdateAccountOwnerDto("owner1"));

        Assert.Equal("owner1", result.OwnerUsername);
        _repo.Verify(r => r.UpdateAsync(account), Times.Once);
    }

    [Fact]
    public async Task UpsertFromSalesAsync_NewCustomer_AddsAccount()
    {
        var customerId = Guid.NewGuid();
        _repo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync((Account a) => a);
        _mapper.Setup(m => m.Map<AccountDto>(It.IsAny<Account>())).Returns((Account a) => Map(a));

        var result = await _sut.UpsertFromSalesAsync(new UpsertAccountDto(
            customerId, "Acme Corp", "TAX-1", "Bill St", "Ship St", DateTimeOffset.UtcNow));

        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal("TAX-1", result.TaxId);
        _repo.Verify(r => r.AddAsync(It.Is<Account>(a => a.CustomerId == customerId)), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertFromSalesAsync_ExistingCustomer_UpdatesSnapshot()
    {
        var customerId = Guid.NewGuid();
        var account = new Account(Guid.NewGuid(), customerId, "Old Name");
        _repo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _repo.Setup(r => r.UpdateAsync(account)).ReturnsAsync(account);
        _mapper.Setup(m => m.Map<AccountDto>(account)).Returns((Account a) => Map(a));

        var result = await _sut.UpsertFromSalesAsync(new UpsertAccountDto(
            customerId, "New Name", "TAX-2", "A", "B", DateTimeOffset.UtcNow));

        Assert.Equal("New Name", result.Name);
        Assert.Equal("TAX-2", result.TaxId);
        _repo.Verify(r => r.UpdateAsync(account), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task UpsertFromSalesAsync_EmptyCustomerId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpsertFromSalesAsync(new UpsertAccountDto(Guid.Empty, "Acme", null, null, null, null)));
    }

    private static AccountDto Map(Account a) =>
        new(a.Id, a.CustomerId, a.Name, a.TaxId, a.BillingAddress, a.ShippingAddress,
            a.IsActive, a.OwnerUsername, a.LastSyncedAt, a.CreatedAt, a.UpdatedAt);
}
