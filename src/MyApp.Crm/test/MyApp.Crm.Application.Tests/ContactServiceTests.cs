using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Tests;

public class ContactServiceTests
{
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IContactRepository> _contacts = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ContactService _sut;

    public ContactServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _sut = new ContactService(
            _accounts.Object,
            _contacts.Object,
            _mapper.Object,
            _uow.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<ContactService>>());
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var contact = new Contact(id, accountId, "Jane Doe", "jane@acme.com");
        var dto = Map(contact);

        _contacts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _mapper.Setup(m => m.Map<ContactDto>(contact)).Returns(dto);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result!.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _contacts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Contact?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListByAccountAsync_ReturnsMappedDtos()
    {
        var accountId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), accountId, "Jane Doe");
        var list = new List<Contact> { contact };

        _contacts.Setup(r => r.ListByAccountAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        _mapper.Setup(m => m.Map<IEnumerable<ContactDto>>(list)).Returns(new[] { Map(contact) });

        var result = (await _sut.ListByAccountAsync(accountId)).ToList();

        Assert.Single(result);
        Assert.Equal("Jane Doe", result[0].FullName);
    }

    [Fact]
    public async Task QueryAsync_ReturnsPaginatedMappedDtos()
    {
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), "Jane Doe");
        var page = new PaginatedResult<Contact>([contact], 1, 20, 1);

        _contacts.Setup(r => r.QueryAsync(It.IsAny<ISpecification<Contact>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        _mapper.Setup(m => m.Map<ContactDto>(contact)).Returns(Map(contact));

        var result = await _sut.QueryAsync(new QuerySpec(1, 20));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Jane Doe", result.Items.First().FullName);
    }

    [Fact]
    public async Task CreateAsync_ExistingAccount_AddsContactAndSaves()
    {
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "Acme");
        var dto = new CreateContactDto(account.Id, "Jane Doe", "jane@acme.com", null, "CEO", true);

        _accounts.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accounts.Setup(r => r.UpdateAsync(account)).ReturnsAsync(account);
        _mapper.Setup(m => m.Map<ContactDto>(It.IsAny<Contact>()))
            .Returns((Contact c) => Map(c));

        var result = await _sut.CreateAsync(dto);

        Assert.Equal("Jane Doe", result.FullName);
        Assert.True(result.IsPrimary);
        Assert.Single(account.Contacts);
        _accounts.Verify(r => r.UpdateAsync(account), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MissingAccount_ThrowsKeyNotFound()
    {
        var accountId = Guid.NewGuid();
        _accounts.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.CreateAsync(new CreateContactDto(accountId, "Jane", null, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), "Old Name");
        _contacts.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _contacts.Setup(r => r.UpdateAsync(contact)).ReturnsAsync(contact);
        _mapper.Setup(m => m.Map<ContactDto>(contact)).Returns((Contact c) => Map(c));

        var result = await _sut.UpdateAsync(contact.Id, new UpdateContactDto("New Name", "n@x.com", "123", "CTO"));

        Assert.Equal("New Name", result.FullName);
        Assert.Equal("n@x.com", result.Email);
        _contacts.Verify(r => r.UpdateAsync(contact), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _contacts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Contact?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.UpdateAsync(id, new UpdateContactDto("Name", null, null, null)));
    }

    [Fact]
    public async Task SetPrimaryAsync_Existing_SetsPrimaryAndSaves()
    {
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "Acme");
        var c1 = account.AddContact(Guid.NewGuid(), "A", null, null, null, isPrimary: true);
        var c2 = account.AddContact(Guid.NewGuid(), "B", null, null, null, isPrimary: false);

        _accounts.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accounts.Setup(r => r.UpdateAsync(account)).ReturnsAsync(account);

        await _sut.SetPrimaryAsync(account.Id, c2.Id);

        Assert.False(c1.IsPrimary);
        Assert.True(c2.IsPrimary);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPrimaryAsync_MissingAccount_ThrowsKeyNotFound()
    {
        var accountId = Guid.NewGuid();
        _accounts.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.SetPrimaryAsync(accountId, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeactivateAsync_Existing_DeactivatesContact()
    {
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), "Jane", isPrimary: true);
        _contacts.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _contacts.Setup(r => r.UpdateAsync(contact)).ReturnsAsync(contact);

        await _sut.DeactivateAsync(contact.Id);

        Assert.False(contact.IsActive);
        Assert.False(contact.IsPrimary);
        _contacts.Verify(r => r.UpdateAsync(contact), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_Missing_DoesNothing()
    {
        var id = Guid.NewGuid();
        _contacts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Contact?)null);

        await _sut.DeactivateAsync(id);

        _contacts.Verify(r => r.UpdateAsync(It.IsAny<Contact>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ContactDto Map(Contact c) =>
        new(c.Id, c.AccountId, c.FullName, c.Email, c.Phone, c.Title, c.IsPrimary, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
