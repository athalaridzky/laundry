using FluentAssertions;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.Services;

namespace RefalinaLaundry.Domain.Tests;

public sealed class PricingCalculatorTests
{
    [Theory]
    [InlineData(ServiceCategory.PerKilo, 7000, 2.5, 17500)]
    [InlineData(ServiceCategory.PerKilo, 9000, 4, 36000)]
    [InlineData(ServiceCategory.PerKilo, 14000, 3, 42000)]
    [InlineData(ServiceCategory.PerItem, 20000, 2, 40000)]
    public void Calculate_WithValidQuantity_ReturnsExpectedTotal(
        ServiceCategory category,
        decimal price,
        decimal quantity,
        decimal expected)
    {
        decimal result = PricingCalculator.Calculate(category, price, quantity, 100m).Amount;

        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_MixedServices_ReturnsExpectedTotal()
    {
        decimal regular = PricingCalculator.Calculate(ServiceCategory.PerKilo, 8000m, 3.2m, 100m).Amount;
        decimal suit = PricingCalculator.Calculate(ServiceCategory.PerItem, 20000m, 1m, 100m).Amount;
        decimal bedcover = PricingCalculator.Calculate(ServiceCategory.PerItem, 30000m, 1m, 100m).Amount;

        (regular + suit + bedcover).Should().Be(75_600m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    [InlineData(2.55)]
    public void Calculate_PerKiloInvalidWeight_Throws(decimal quantity)
    {
        Action action = () => PricingCalculator.Calculate(ServiceCategory.PerKilo, 7000m, quantity, 100m);

        action.Should().Throw<DomainRuleException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.5)]
    public void Calculate_PerItemInvalidQuantity_Throws(decimal quantity)
    {
        Action action = () => PricingCalculator.Calculate(ServiceCategory.PerItem, 20_000m, quantity, 100m);

        action.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OrderItem_WhenServicePriceChanges_KeepsSnapshot()
    {
        DateTime now = DateTime.UtcNow;
        Service service = new("Reguler 3 Hari", ServiceCategory.PerKilo, 7000m, 3, true, now);
        OrderItem item = new(service, 2.5m, 100m);

        service.Update("Reguler 3 Hari", ServiceCategory.PerKilo, 8000m, 3, true, now.AddMinutes(1));

        item.UnitPriceSnapshot.Should().Be(7000m);
        item.Subtotal.Should().Be(17_500m);
    }
}
