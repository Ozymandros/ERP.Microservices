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
            .Returns((Account a) => new AccountDto(
                a.Id, a.CustomerId, a.Name, a.TaxId, a.BillingAddress, a.ShippingAddress,
                a.IsActive, a.OwnerUsername, a.LastSyncedAt, a.CreatedAt, a.UpdatedAt));

        var result = await _sut.UpdateOwnerAsync(id, new UpdateAccountOwnerDto("owner1"));

        Assert.Equal("owner1", result.OwnerUsername);
        _repo.Verify(r => r.UpdateAsync(account), Times.Once);
    }
}
