using FluentAssertions;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.ValueObjects;

namespace RefalinaLaundry.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("0812-3456-7890", "6281234567890")]
    [InlineData("+62 812 3456 7890", "6281234567890")]
    [InlineData("81234567890", "6281234567890")]
    public void PhoneNumber_NormalizesIndonesianFormat(string input, string expected)
    {
        new PhoneNumber(input).Value.Should().Be(expected);
    }

    [Fact]
    public void Weight_ValidStep_KeepsKilograms()
    {
        new Weight(3.2m, 100m).Kilograms.Should().Be(3.2m);
    }

    [Fact]
    public void Weight_AboveMaximum_Throws()
    {
        Action action = () => _ = new Weight(100.1m, 100m);

        action.Should().Throw<DomainRuleException>();
    }
}
