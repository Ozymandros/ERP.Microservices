using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Sales.API.Controllers;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Sales.API.Tests.Controllers;

/// <summary>Unit tests for <see cref="CustomersController"/> covering happy paths, NotFound, and key status codes.</summary>
public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _customerService;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<CustomersController>> _logger;
    private readonly CustomersController _sut;

    public CustomersControllerTests()
    {
        _customerService = new Mock<ICustomerService>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<CustomersController>>();
        _sut = new CustomersController(_customerService.Object, _cacheService.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static CustomerDto SampleCustomer(Guid? id = null) =>
        new(id ?? Guid.NewGuid())
        {
            Name = "Acme Corp",
            Email = "billing@acme.test",
            PhoneNumber = "+15551212",
            Address = "1 Main St"
        };

    // ─── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoQuery_CacheHit_ReturnsCached()
    {
        var cached = new List<CustomerDto> { SampleCustomer() };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CustomerDto>>("all_customers"))
            .ReturnsAsync(cached);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _customerService.Verify(s => s.ListCustomersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_NoQuery_CacheMiss_Returns200AndCaches()
    {
        var customers = new List<CustomerDto> { SampleCustomer(), SampleCustomer() };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CustomerDto>>("all_customers"))
            .ReturnsAsync((IEnumerable<CustomerDto>?)null);
        _customerService.Setup(s => s.ListCustomersAsync())
            .ReturnsAsync(customers);
        _cacheService.Setup(c => c.SaveStateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<CustomerDto>>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(customers);
        _cacheService.Verify(
            c => c.SaveStateAsync(
                "all_customers",
                It.Is<IEnumerable<CustomerDto>>(x => ReferenceEquals(x, customers)),
                It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WithQueryParams_QueriesService()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");
        var paginated = new PaginatedResult<CustomerDto>(new List<CustomerDto> { SampleCustomer() }, 1, 10, 1);
        _customerService.Setup(s => s.QueryCustomersAsync(It.IsAny<ISpecification<MyApp.Sales.Domain.Entities.Customer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _sut.GetAll(new QuerySpec { Page = 1, PageSize = 10 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(paginated);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheHit_Returns200()
    {
        var id = Guid.NewGuid();
        var cached = SampleCustomer(id);
        _cacheService.Setup(c => c.GetStateAsync<CustomerDto>($"Customer-{id}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _customerService.Verify(s => s.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_CacheMiss_Exists_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = SampleCustomer(id);
        _cacheService.Setup(c => c.GetStateAsync<CustomerDto>($"Customer-{id}"))
            .ReturnsAsync((CustomerDto?)null);
        _customerService.Setup(s => s.GetCustomerByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        _cacheService.Setup(c => c.SaveStateAsync($"Customer-{id}", dto, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<CustomerDto>($"Customer-{id}"))
            .ReturnsAsync((CustomerDto?)null);
        _customerService.Setup(s => s.GetCustomerByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Create / Update / Delete ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = SampleCustomer();
        var created = SampleCustomer();
        _customerService.Setup(s => s.CreateCustomerAsync(dto)).ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync("all_customers")).Returns(Task.CompletedTask);

        var result = await _sut.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(created);
        _cacheService.Verify(c => c.RemoveStateAsync("all_customers"), Times.Once);
    }

    [Fact]
    public async Task Create_InvalidModel_Returns400()
    {
        _sut.ModelState.AddModelError("Name", "Required");

        var result = await _sut.Create(SampleCustomer());

        result.Should().BeOfType<BadRequestObjectResult>();
        _customerService.Verify(s => s.CreateCustomerAsync(It.IsAny<CustomerDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_ValidDto_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = new CreateUpdateCustomerDto { Name = "Updated", Email = "u@test.com" };
        var updated = SampleCustomer(id);
        _customerService.Setup(s => s.UpdateCustomerAsync(id, dto)).ReturnsAsync(updated);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(updated);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = new CreateUpdateCustomerDto { Name = "X", Email = "x@test.com" };
        _customerService.Setup(s => s.UpdateCustomerAsync(id, dto))
            .ThrowsAsync(new InvalidOperationException("Customer not found"));

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        var id = Guid.NewGuid();
        _customerService.Setup(s => s.DeleteCustomerAsync(id)).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _customerService.Setup(s => s.DeleteCustomerAsync(id))
            .ThrowsAsync(new InvalidOperationException("Customer not found"));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
