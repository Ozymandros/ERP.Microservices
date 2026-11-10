using FluentAssertions;
using MyApp.Projects.Domain.Entities;
using Xunit;

namespace MyApp.Projects.Domain.Tests.Entities;

/// <summary>Tests for <see cref="ProjectPlanningLine"/> amounts and quantity bookkeeping.</summary>
public class ProjectPlanningLineTests
{
    private static ProjectPlanningLine ResourceLine(decimal quantity = 10m, PlanningLineType type = PlanningLineType.BudgetAndBillable) =>
        new(Guid.NewGuid())
        {
            Kind = ProjectLineKind.Resource,
            ResourceId = Guid.NewGuid(),
            LineType = type,
            Quantity = quantity,
            UnitCostLcy = 40m,
            UnitPrice = 100m,
            LineDiscountPercent = 10m,
            CurrencyCode = "USD",
            ExchangeRate = 0.9m
        };

    [Fact]
    public void Recalculate_ComputesCostAndAmountsInBothCurrencies()
    {
        var line = ResourceLine(7.5m);

        line.Recalculate();

        line.TotalCostLcy.Should().Be(300m);          // 7.5 * 40
        line.LineAmount.Should().Be(675m);            // 7.5 * 100 - 10%
        line.LineAmountLcy.Should().Be(607.5m);       // 675 * 0.9
        line.NetUnitPrice.Should().Be(90m);
    }

    [Fact]
    public void Recalculate_TextLine_ZeroesAmounts()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            Kind = ProjectLineKind.Text, Description = "Note", Quantity = 5, UnitPrice = 10, UnitCostLcy = 3
        };

        line.Recalculate();

        line.Quantity.Should().Be(0);
        line.LineAmount.Should().Be(0);
        line.TotalCostLcy.Should().Be(0);
    }

    [Fact]
    public void QuantityToInvoice_BudgetOnlyLine_IsZero()
    {
        ResourceLine(type: PlanningLineType.Budget).QuantityToInvoice.Should().Be(0);
    }

    [Fact]
    public void TransferToInvoice_ReducesQuantityToInvoice_AndCannotExceedIt()
    {
        var line = ResourceLine(10m);

        line.TransferToInvoice(4m);

        line.QuantityTransferredToInvoice.Should().Be(4m);
        line.QuantityToInvoice.Should().Be(6m);
        var act = () => line.TransferToInvoice(6.5m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReverseTransferToInvoice_RestoresQuantity()
    {
        var line = ResourceLine(10m);
        line.TransferToInvoice(10m);

        line.ReverseTransferToInvoice(3m);

        line.QuantityToInvoice.Should().Be(3m);
    }

    [Fact]
    public void Consumption_ReserveConfirmRelease_TracksQuantities()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            Kind = ProjectLineKind.Item, ProductId = Guid.NewGuid(), Quantity = 10m
        };

        line.ReserveForConsumption(6m);
        line.RemainingQuantity.Should().Be(4m);

        line.ConfirmConsumption(4m);
        line.QuantityPosted.Should().Be(4m);
        line.QuantityInConsumption.Should().Be(2m);

        line.ReleaseConsumption(2m);
        line.QuantityInConsumption.Should().Be(0m);
        line.RemainingQuantity.Should().Be(6m);
    }

    [Fact]
    public void ReserveForConsumption_MoreThanRemaining_Throws()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid()) { Kind = ProjectLineKind.Item, ProductId = Guid.NewGuid(), Quantity = 2m };

        var act = () => line.ReserveForConsumption(3m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_ItemWithFractionalQuantity_Throws()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid()) { Kind = ProjectLineKind.Item, ProductId = Guid.NewGuid(), Quantity = 1.5m };

        var act = () => line.Validate();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_QuantityBelowTransferred_Throws()
    {
        var line = ResourceLine(10m);
        line.TransferToInvoice(8m);
        line.Quantity = 5m;

        var act = () => line.Validate();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_ResourceLineWithoutResource_Throws()
    {
        var line = ResourceLine();
        line.ResourceId = null;

        var act = () => line.Validate();

        act.Should().Throw<ArgumentException>();
    }
}
