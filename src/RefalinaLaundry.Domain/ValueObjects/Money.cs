using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.ValueObjects;

public readonly record struct Money
{
    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainRuleException("money.negative", "Nilai uang tidak boleh negatif.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public decimal Amount { get; }

    public static Money Zero => new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right)
    {
        if (right.Amount > left.Amount)
        {
            throw new DomainRuleException("money.insufficient", "Nilai pengurang melebihi saldo.");
        }

        return new Money(left.Amount - right.Amount);
    }

    public override string ToString() => Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
