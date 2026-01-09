using AutoMapper;
using Moq;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Services;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using Xunit;

namespace MyApp.Sales.Application.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CustomerService _customerService;

    public CustomerServiceTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockMapper = new Mock<IMapper>();

        _customerService = new CustomerService(
            _mockCustomerRepository.Object,
            _mockMapper.Object);
    }

    #region GetCustomerByIdAsync Tests

    [Fact]
    public async Task GetCustomerByIdAsync_WithExistingId_ReturnsCustomerDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId)
        {
            Name = "Test Customer",
            Email = "test@example.com"
        };

        var expectedDto = new CustomerDto(customerId)
        {
            Name = "Test Customer",
            Email = "test@example.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<CustomerDto>(customer)).Returns(expectedDto);

        // Act
        var result = await _customerService.GetCustomerByIdAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Customer", result.Name);
        Assert.Equal("test@example.com", result.Email);

        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockMapper.Verify(m => m.Map<CustomerDto>(customer), Times.Once);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act
        var result = await _customerService.GetCustomerByIdAsync(customerId);

        // Assert
        Assert.Null(result);
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockMapper.Verify(m => m.Map<CustomerDto>(It.IsAny<Customer>()), Times.Never);
    }

    #endregion

    #region ListCustomersAsync Tests

    [Fact]
    public async Task ListCustomersAsync_ReturnsAllCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new Customer(Guid.NewGuid()) { Name = "Customer 1", Email = "c1@example.com" },
            new Customer(Guid.NewGuid()) { Name = "Customer 2", Email = "c2@example.com" }
        };

        var customerDtos = new List<CustomerDto>
        {
            new CustomerDto(Guid.NewGuid())
            {
                Name = "Customer 1",
                Email = "c1@example.com",
                PhoneNumber = "",
                Address = ""
            },
            new CustomerDto(Guid.NewGuid())
            {
                Name = "Customer 2",
                Email = "c2@example.com",
                PhoneNumber = "",
                Address = ""
            }
        };

        _mockCustomerRepository.Setup(r => r.ListAsync()).ReturnsAsync(customers);
        _mockMapper.Setup(m => m.Map<IEnumerable<CustomerDto>>(customers)).Returns(customerDtos);

        // Act
        var result = await _customerService.ListCustomersAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, c => c.Name == "Customer 1");
        Assert.Contains(resultList, c => c.Name == "Customer 2");

        _mockCustomerRepository.Verify(r => r.ListAsync(), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<CustomerDto>>(customers), Times.Once);
    }

    [Fact]
    public async Task ListCustomersAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockCustomerRepository.Setup(r => r.ListAsync()).ReturnsAsync(new List<Customer>());
        _mockMapper.Setup(m => m.Map<IEnumerable<CustomerDto>>(It.IsAny<List<Customer>>()))
            .Returns(new List<CustomerDto>());

        // Act
        var result = await _customerService.ListCustomersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockCustomerRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    #endregion

    #region CreateCustomerAsync Tests

    [Fact]
    public async Task CreateCustomerAsync_WithValidDto_CreatesCustomer()
    {
        // Arrange
        var dto = new CustomerDto(Guid.NewGuid())
        {
            Name = "New Customer",
            Email = "new@example.com",
            PhoneNumber = "",
            Address = ""
        };

        var customer = new Customer(Guid.NewGuid())
        {
            Name = "New Customer",
            Email = "new@example.com"
        };

        var expectedDto = new CustomerDto(Guid.NewGuid())
        {
            Name = "New Customer",
            Email = "new@example.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockMapper.Setup(m => m.Map<Customer>(dto)).Returns(customer);
        _mockMapper.Setup(m => m.Map<CustomerDto>(It.IsAny<Customer>())).Returns(expectedDto);

        // Act
        var result = await _customerService.CreateCustomerAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Customer", result.Name);

        _mockCustomerRepository.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.Id != Guid.Empty
        )), Times.Once);
        _mockMapper.Verify(m => m.Map<Customer>(dto), Times.Once);
    }

    [Fact]
    public async Task CreateCustomerAsync_AssignsNewGuid()
    {
        // Arrange
        var dto = new CustomerDto(Guid.NewGuid())
        {
            Name = "Test",
            Email = "test@example.com",
            PhoneNumber = "",
            Address = ""
        };
        var customer = new Customer(Guid.NewGuid()) { Name = "Test", Email = "test@example.com" };

        _mockMapper.Setup(m => m.Map<Customer>(dto)).Returns(customer);
        _mockMapper.Setup(m => m.Map<CustomerDto>(It.IsAny<Customer>())).Returns(new CustomerDto(Guid.NewGuid())
        {
            Name = "Test Name",
            Email = "test@email.com",
            PhoneNumber = "",
            Address = ""
        });

        // Act
        await _customerService.CreateCustomerAsync(dto);

        // Assert
        _mockCustomerRepository.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.Id != Guid.Empty
        )), Times.Once);
    }

    #endregion

    #region UpdateCustomerAsync Tests

    [Fact]
    public async Task UpdateCustomerAsync_WithExistingCustomer_UpdatesSuccessfully()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = new Customer(customerId)
        {
            Name = "Old Name",
            Email = "old@example.com"
        };

        var updateDto = new CustomerDto(Guid.NewGuid())
        {
            Name = "New Name",
            Email = "new@example.com",
            PhoneNumber = "",
            Address = ""
        };

        var expectedDto = new CustomerDto(customerId)
        {
            Name = "New Name",
            Email = "new@example.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(existingCustomer);
        _mockMapper.Setup(m => m.Map(updateDto, existingCustomer)).Callback(() =>
        {
            existingCustomer.Name = updateDto.Name;
            existingCustomer.Email = updateDto.Email;
        });
        _mockMapper.Setup(m => m.Map<CustomerDto>(existingCustomer)).Returns(expectedDto);

        // Act
        var result = await _customerService.UpdateCustomerAsync(customerId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);

        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockCustomerRepository.Verify(r => r.UpdateAsync(existingCustomer), Times.Once);
        _mockMapper.Verify(m => m.Map(updateDto, existingCustomer), Times.Once);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithNonExistentCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var updateDto = new CustomerDto(Guid.NewGuid())
        {
            Name = "Test",
            Email = "test@example.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _customerService.UpdateCustomerAsync(customerId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockCustomerRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>()), Times.Never);
    }

    #endregion

    #region DeleteCustomerAsync Tests

    [Fact]
    public async Task DeleteCustomerAsync_CallsRepositoryDeleteWithCorrectId()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        await _customerService.DeleteCustomerAsync(customerId);

        // Assert
        _mockCustomerRepository.Verify(r => r.DeleteAsync(customerId), Times.Once);
    }

    #endregion
}
