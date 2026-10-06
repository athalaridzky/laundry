using FluentAssertions;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.Services;

namespace RefalinaLaundry.Domain.Tests;

public sealed class PaymentRulesTests
{
    [Theory]
    [InlineData(17500, 0, PaymentStatus.Unpaid)]
    [InlineData(17500, 10000, PaymentStatus.PartiallyPaid)]
    [InlineData(17500, 17500, PaymentStatus.Paid)]
    public void GetStatus_ReturnsStatusFromAmounts(decimal total, decimal paid, PaymentStatus expected)
    {
        PaymentRules.GetStatus(total, paid).Should().Be(expected);
    }

    [Fact]
    public void CalculateAppliedAmount_CashSettlementWithExtraTender_AppliesOutstandingOnly()
    {
        decimal applied = PaymentRules.CalculateAppliedAmount(17_500m, 17_500m, PaymentMethod.Cash, 20_000m);

        applied.Should().Be(17_500m);
        (20_000m - applied).Should().Be(2_500m);
    }

    [Fact]
    public void CalculateAppliedAmount_NonCashOverpayment_Throws()
    {
        Action action = () => PaymentRules.CalculateAppliedAmount(7_500m, 8_000m, PaymentMethod.Transfer, null);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*tidak boleh melebihi*");
    }

    [Fact]
    public void CalculateAppliedAmount_InsufficientCash_Throws()
    {
        Action action = () => PaymentRules.CalculateAppliedAmount(17_500m, 17_500m, PaymentMethod.Cash, 10_000m);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*kurang*");
    }
}
