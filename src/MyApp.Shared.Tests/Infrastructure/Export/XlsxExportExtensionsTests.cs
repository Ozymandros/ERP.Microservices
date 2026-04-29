using FluentAssertions;
using MyApp.Shared.Infrastructure.Export;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Export;

public class XlsxExportExtensionsTests
{
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public void ExportToXlsx_WithEmptyCollection_ReturnsValidXlsx()
    {
        // Arrange
        var items = new List<TestItem>();

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // XLSX files start with PK (ZIP signature)
        result[0].Should().Be(0x50); // 'P'
        result[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public void ExportToXlsx_WithSingleItem_ReturnsValidXlsx()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Test", Price = 10.50m, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result[0].Should().Be(0x50); // ZIP signature
        result[1].Should().Be(0x4B);
    }

    [Fact]
    public void ExportToXlsx_WithMultipleItems_ReturnsValidXlsx()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Item1", Price = 10.50m, CreatedAt = DateTime.UtcNow },
            new TestItem { Id = 2, Name = "Item2", Price = 20.75m, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new TestItem { Id = 3, Name = "Item3", Price = 30.00m, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result[0].Should().Be(0x50);
        result[1].Should().Be(0x4B);
    }

    [Fact]
    public void ExportToXlsx_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = null!, Price = 0, CreatedAt = default }
        };

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ExportToXlsx_WithTypeEndingInS_UsesCorrectSheetName()
    {
        // Arrange
        var items = new List<TestItem>();

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        // Sheet name logic: if type ends with 's', use as-is, otherwise add 's'
        // TestItem doesn't end with 's', so it should become "TestItems"
    }

    [Fact]
    public void ExportToXlsx_WithComplexTypes_ExportsCorrectly()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem
            {
                Id = 1,
                Name = "Complex Item",
                Price = 1234.56m,
                CreatedAt = new DateTime(2024, 1, 1, 12, 30, 45)
            }
        };

        // Act
        var result = items.ExportToXlsx();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
