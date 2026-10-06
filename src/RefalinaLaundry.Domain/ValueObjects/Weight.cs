using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.ValueObjects;

public readonly record struct Weight
{
    public const decimal StepKg = 0.1m;

    public Weight(decimal kilograms, decimal maximumKilograms)
    {
        if (kilograms <= 0)
        {
            throw new DomainRuleException("weight.non_positive", "Berat harus lebih dari 0 kg.");
        }

        if (kilograms > maximumKilograms)
        {
            throw new DomainRuleException("weight.maximum", $"Berat tidak boleh melebihi {maximumKilograms:0.#} kg.");
        }

        if (decimal.Remainder(kilograms, StepKg) != 0)
        {
            throw new DomainRuleException("weight.step", "Berat harus menggunakan kelipatan 0,1 kg.");
        }

        Kilograms = decimal.Round(kilograms, 3, MidpointRounding.AwayFromZero);
    }

    public decimal Kilograms { get; }
}
