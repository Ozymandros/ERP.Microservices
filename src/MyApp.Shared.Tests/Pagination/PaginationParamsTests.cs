using FluentAssertions;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Shared.Tests.Pagination;

public class PaginationParamsTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultPageNumberAndPageSize()
    {
        var sut = new PaginationParams();

        sut.PageNumber.Should().Be(1);
        sut.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithValidValues_SetsValues()
    {
        var sut = new PaginationParams(3, 25);

        sut.PageNumber.Should().Be(3);
        sut.PageSize.Should().Be(25);
    }

    [Fact]
    public void Constructor_WithZeroOrNegativePageNumber_NormalizesToOne()
    {
        new PaginationParams(0, 10).PageNumber.Should().Be(1);
        new PaginationParams(-5, 10).PageNumber.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithZeroOrNegativePageSize_NormalizesToTen()
    {
        new PaginationParams(1, 0).PageSize.Should().Be(10);
        new PaginationParams(1, -3).PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithPageSizeAboveMax_CapsAtOneHundred()
    {
        var sut = new PaginationParams(1, 250);

        sut.PageSize.Should().Be(100);
    }

    [Fact]
    public void Constructor_WithExactMaxPageSize_KeepsOneHundred()
    {
        var sut = new PaginationParams(1, 100);

        sut.PageSize.Should().Be(100);
    }
}
