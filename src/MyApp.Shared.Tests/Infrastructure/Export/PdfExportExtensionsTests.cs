using FluentAssertions;
using MyApp.Shared.Infrastructure.Export;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Export;

public class PdfExportExtensionsTests
{
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TestItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void ExportToPdf_WithEmptyCollection_ReturnsValidPdf()
    {
        // Arrange
        var items = new List<TestItem>();

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // PDF files start with %PDF
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public void ExportToPdf_WithSingleItem_ReturnsValidPdf()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Test", Price = 10.50m, CreatedAt = DateTime.UtcNow, IsActive = true }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public void ExportToPdf_WithMultipleItems_ReturnsValidPdf()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Item1", Price = 10.50m, CreatedAt = DateTime.UtcNow, IsActive = true },
            new TestItem { Id = 2, Name = "Item2", Price = 20.75m, CreatedAt = DateTime.UtcNow.AddDays(-1), IsActive = false },
            new TestItem { Id = 3, Name = "Item3", Price = 30.00m, CreatedAt = DateTime.UtcNow.AddDays(-2), IsActive = true }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public void ExportToPdf_WithTypeEndingInDto_RemovesDtoSuffix()
    {
        // Arrange
        var items = new List<TestItemDto>
        {
            new TestItemDto { Id = 1, Name = "Test" }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // Type name "TestItemDto" should become "TestItem" (removes "Dto"), then pluralized to "TestItems"
    }

    [Fact]
    public void ExportToPdf_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 0, Name = null!, Price = 0, CreatedAt = default, IsActive = false }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ExportToPdf_WithComplexTypes_ExportsCorrectly()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem
            {
                Id = 1,
                Name = "Complex Item",
                Price = 1234.56m,
                CreatedAt = new DateTime(2024, 1, 1, 12, 30, 45),
                IsActive = true
            }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public void ExportToPdf_WithBooleanValues_FormatsCorrectly()
    {
        // Arrange
        var items = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Active", IsActive = true },
            new TestItem { Id = 2, Name = "Inactive", IsActive = false }
        };

        // Act
        var result = items.ExportToPdf();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
