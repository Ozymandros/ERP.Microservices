using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.API.Controllers;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Caching;
using Xunit;

namespace MyApp.Inventory.API.Tests.Controllers;

public class InventoryTransactionsControllerTests
{
    private readonly Mock<IInventoryTransactionService> _transactionService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<InventoryTransactionsController>> _logger = new();
    private readonly InventoryTransactionsController _sut;

    public InventoryTransactionsControllerTests()
    {
        _sut = new InventoryTransactionsController(_transactionService.Object, _logger.Object, _cacheService.Object);
    }

  private static InventoryTransactionDto SampleTxn(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, TransactionType.Adjustment, DateTime.UtcNow);

    [Fact]
    public async Task GetAllTransactions_ReturnsList()
    {
        var txns = new List<InventoryTransactionDto> { SampleTxn() };
        _transactionService.Setup(s => s.GetAllTransactionsAsync()).ReturnsAsync(txns);

        var result = await _sut.GetAllTransactions();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(txns);
    }

    [Fact]
    public async Task GetTransactionById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _transactionService.Setup(s => s.GetTransactionByIdAsync(id)).ReturnsAsync((InventoryTransactionDto?)null);

        var result = await _sut.GetTransactionById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTransactionById_Found_ReturnsTransaction()
    {
        var txn = SampleTxn();
        _transactionService.Setup(s => s.GetTransactionByIdAsync(txn.Id)).ReturnsAsync(txn);

        var result = await _sut.GetTransactionById(txn.Id);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(txn);
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        _transactionService.Setup(s => s.GetAllTransactionsAsync())
            .ReturnsAsync(new List<InventoryTransactionDto> { SampleTxn() });

        var result = await _sut.ExportToXlsx();

        result.Should().BeOfType<FileContentResult>();
    }
}