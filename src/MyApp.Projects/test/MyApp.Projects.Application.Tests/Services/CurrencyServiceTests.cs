using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

public class CurrencyServiceTests
{
    private readonly Mock<ICurrencyRepository> _currencies = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CurrencyService _sut;

    public CurrencyServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _mapper.Setup(m => m.Map<CurrencyDto>(It.IsAny<Currency>()))
            .Returns((Currency c) => new CurrencyDto(c.Id)
            {
                Code = c.Code,
                Description = c.Description,
                Symbol = c.Symbol,
                ExchangeRates = c.ExchangeRates
                    .Select(r => new ExchangeRateDto(r.Id)
                    {
                        CurrencyId = r.CurrencyId,
                        StartingDate = r.StartingDate,
                        RateToLocal = r.RateToLocal
                    }).ToList()
            });
        _mapper.Setup(m => m.Map<ExchangeRateDto>(It.IsAny<ExchangeRate>()))
            .Returns((ExchangeRate r) => new ExchangeRateDto(r.Id)
            {
                CurrencyId = r.CurrencyId,
                StartingDate = r.StartingDate,
                RateToLocal = r.RateToLocal
            });

        _sut = new CurrencyService(
            _currencies.Object,
            _mapper.Object,
            new ProjectsSettings { LocalCurrency = "EUR" },
            _uow.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<CurrencyService>>());
    }

    [Fact]
    public async Task ListAsync_MarksLocalCurrency()
    {
        var eur = new Currency(Guid.NewGuid()) { Code = "EUR", Description = "Euro" };
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "US Dollar" };
        _currencies.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([eur, usd]);

        var result = await _sut.ListAsync();

        result.Should().Contain(c => c.Code == "EUR" && c.IsLocalCurrency);
        result.Should().Contain(c => c.Code == "USD" && !c.IsLocalCurrency);
    }

    [Fact]
    public async Task GetByCodeAsync_Existing_ReturnsDto()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "US Dollar", Symbol = "$" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);

        var result = await _sut.GetByCodeAsync("USD");

        result.Should().NotBeNull();
        result!.Description.Should().Be("US Dollar");
        result.IsLocalCurrency.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCodeAsync_Missing_ReturnsNull()
    {
        _currencies.Setup(r => r.GetByCodeAsync("GBP", It.IsAny<CancellationToken>())).ReturnsAsync((Currency?)null);

        var result = await _sut.GetByCodeAsync("GBP");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsCurrency()
    {
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync((Currency?)null);
        _currencies.Setup(r => r.AddAsync(It.IsAny<Currency>())).ReturnsAsync((Currency c) => c);

        var result = await _sut.CreateAsync(new CreateUpdateCurrencyDto
        {
            Code = "usd",
            Description = " US Dollar ",
            Symbol = "$"
        });

        result.Code.Should().Be("USD");
        result.Description.Should().Be("US Dollar");
        _currencies.Verify(r => r.AddAsync(It.Is<Currency>(c => c.Code == "USD")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Duplicate_Throws()
    {
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Currency(Guid.NewGuid()) { Code = "USD" });

        var act = () => _sut.CreateAsync(new CreateUpdateCurrencyDto { Code = "USD", Description = "Dollar" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesDescription()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Old" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);

        var result = await _sut.UpdateAsync("USD", new CreateUpdateCurrencyDto
        {
            Code = "USD",
            Description = " New Dollar ",
            Symbol = "$"
        });

        result.Description.Should().Be("New Dollar");
        usd.Symbol.Should().Be("$");
    }

    [Fact]
    public async Task UpdateAsync_CodeChange_Throws()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);

        var act = () => _sut.UpdateAsync("USD", new CreateUpdateCurrencyDto { Code = "GBP", Description = "Pound" });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be changed*");
    }

    [Fact]
    public async Task DeleteAsync_LocalCurrency_Throws()
    {
        var eur = new Currency(Guid.NewGuid()) { Code = "EUR", Description = "Euro" };
        _currencies.Setup(r => r.GetByCodeAsync("EUR", It.IsAny<CancellationToken>())).ReturnsAsync(eur);

        var act = () => _sut.DeleteAsync("EUR");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*local currency*");
    }

    [Fact]
    public async Task DeleteAsync_InUse_Throws()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);
        _currencies.Setup(r => r.IsInUseAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.DeleteAsync("USD");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*used by*");
    }

    [Fact]
    public async Task DeleteAsync_Unused_Deletes()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);
        _currencies.Setup(r => r.IsInUseAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _currencies.Setup(r => r.DeleteAsync(usd)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync("USD");

        _currencies.Verify(r => r.DeleteAsync(usd), Times.Once);
    }

    [Fact]
    public async Task AddExchangeRateAsync_Valid_AddsRate()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);
        _currencies.Setup(r => r.AddExchangeRateAsync(It.IsAny<ExchangeRate>())).Returns(Task.CompletedTask);

        var result = await _sut.AddExchangeRateAsync("USD", new CreateExchangeRateDto
        {
            StartingDate = new DateTime(2026, 1, 1),
            RateToLocal = 0.9m
        });

        result.RateToLocal.Should().Be(0.9m);
        result.StartingDate.Should().Be(new DateTime(2026, 1, 1));
        _currencies.Verify(r => r.AddExchangeRateAsync(It.Is<ExchangeRate>(x => x.CurrencyId == usd.Id)), Times.Once);
    }

    [Fact]
    public async Task AddExchangeRateAsync_LocalCurrency_Throws()
    {
        var eur = new Currency(Guid.NewGuid()) { Code = "EUR", Description = "Euro" };
        _currencies.Setup(r => r.GetByCodeAsync("EUR", It.IsAny<CancellationToken>())).ReturnsAsync(eur);

        var act = () => _sut.AddExchangeRateAsync("EUR", new CreateExchangeRateDto
        {
            StartingDate = DateTime.UtcNow,
            RateToLocal = 1m
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*does not have exchange rates*");
    }

    [Fact]
    public async Task AddExchangeRateAsync_NonPositiveRate_Throws()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);

        var act = () => _sut.AddExchangeRateAsync("USD", new CreateExchangeRateDto
        {
            StartingDate = DateTime.UtcNow,
            RateToLocal = 0m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task DeleteExchangeRateAsync_Matching_RemovesRate()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        var rate = new ExchangeRate(Guid.NewGuid()) { CurrencyId = usd.Id, StartingDate = DateTime.UtcNow.Date, RateToLocal = 0.9m };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);
        _currencies.Setup(r => r.GetExchangeRateAsync(rate.Id, It.IsAny<CancellationToken>())).ReturnsAsync(rate);

        await _sut.DeleteExchangeRateAsync("USD", rate.Id);

        _currencies.Verify(r => r.RemoveExchangeRate(rate), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteExchangeRateAsync_WrongCurrency_ThrowsNotFound()
    {
        var usd = new Currency(Guid.NewGuid()) { Code = "USD", Description = "Dollar" };
        var rate = new ExchangeRate(Guid.NewGuid()) { CurrencyId = Guid.NewGuid(), RateToLocal = 0.9m };
        _currencies.Setup(r => r.GetByCodeAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(usd);
        _currencies.Setup(r => r.GetExchangeRateAsync(rate.Id, It.IsAny<CancellationToken>())).ReturnsAsync(rate);

        var act = () => _sut.DeleteExchangeRateAsync("USD", rate.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRateAsync_LocalCurrency_ReturnsOne()
    {
        var rate = await _sut.GetRateAsync("eur", DateTime.UtcNow);

        rate.Should().Be(1m);
        _currencies.Verify(r => r.GetRateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRateAsync_Foreign_ReturnsRepositoryRate()
    {
        _currencies.Setup(r => r.GetRateAsync("USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0.92m);

        var rate = await _sut.GetRateAsync("usd", new DateTime(2026, 3, 1));

        rate.Should().Be(0.92m);
    }

    [Fact]
    public async Task GetRateAsync_Missing_Throws()
    {
        _currencies.Setup(r => r.GetRateAsync("USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((decimal?)null);

        var act = () => _sut.GetRateAsync("USD", new DateTime(2026, 3, 1));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No exchange rate*");
    }

    [Fact]
    public async Task GetRateAsync_InvalidCode_Throws()
    {
        var act = () => _sut.GetRateAsync("US", DateTime.UtcNow);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*3-letter*");
    }
}
