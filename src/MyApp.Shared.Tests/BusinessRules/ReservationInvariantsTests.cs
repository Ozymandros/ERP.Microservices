using FluentAssertions;
using MyApp.Shared.Domain.BusinessRules;
using Xunit;

namespace MyApp.Shared.Tests.BusinessRules;

public class ReservationInvariantsTests
{
    #region IsReservationExpired Tests

    [Fact]
    public void IsReservationExpired_WithFutureDate_ReturnsFalse()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddHours(1);

        // Act
        var result = ReservationInvariants.IsReservationExpired(futureDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsReservationExpired_WithPastDate_ReturnsTrue()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = ReservationInvariants.IsReservationExpired(pastDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsReservationExpired_WithCurrentDate_ReturnsTrue()
    {
        // Arrange
        var currentDate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = ReservationInvariants.IsReservationExpired(currentDate);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsQuantityValid Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsQuantityValid_WithVariousQuantities_ReturnsExpectedResult(
        int quantity, bool expected)
    {
        // Act
        var result = ReservationInvariants.IsQuantityValid(quantity);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ValidateReservation Tests

    [Fact]
    public void ValidateReservation_WithValidReservation_DoesNotThrow()
    {
        // Arrange
        var quantity = 10;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddHours(24);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateReservation_WithZeroQuantity_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantity = 0;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddHours(24);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Reservation quantity must be greater than zero");
    }

    [Fact]
    public void ValidateReservation_WithNegativeQuantity_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantity = -1;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddHours(24);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Reservation quantity must be greater than zero");
    }

    [Fact]
    public void ValidateReservation_WithQuantityExceedingAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantity = 101;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddHours(24);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reserve 101 units. Only 100 available");
    }

    [Fact]
    public void ValidateReservation_WithExpiredDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantity = 10;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Reservation expiry date must be in the future");
    }

    [Fact]
    public void ValidateReservation_WithCurrentDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantity = 10;
        var availableQuantity = 100;
        var reservedUntil = DateTime.UtcNow.AddSeconds(-1);

        // Act & Assert
        Action act = () => ReservationInvariants.ValidateReservation(quantity, availableQuantity, reservedUntil);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Reservation expiry date must be in the future");
    }

    #endregion

    #region CalculateReservationExpiry Tests

    [Fact]
    public void CalculateReservationExpiry_ReturnsFutureDate()
    {
        // Act
        var result = ReservationInvariants.CalculateReservationExpiry();

        // Assert
        result.Should().BeAfter(DateTime.UtcNow);
        result.Should().BeCloseTo(DateTime.UtcNow.Add(ReservationInvariants.DefaultReservationDuration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CalculateReservationExpiry_UsesDefaultReservationDuration()
    {
        // Act
        var result = ReservationInvariants.CalculateReservationExpiry();
        var expectedExpiry = DateTime.UtcNow.Add(ReservationInvariants.DefaultReservationDuration);

        // Assert
        result.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CalculateReservationExpiry_DefaultDurationIs24Hours()
    {
        // Assert
        ReservationInvariants.DefaultReservationDuration.Should().Be(TimeSpan.FromHours(24));
    }

    #endregion
}
