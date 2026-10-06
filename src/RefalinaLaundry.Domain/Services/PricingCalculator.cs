using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.ValueObjects;

namespace RefalinaLaundry.Domain.Services;

public static class PricingCalculator
{
    public static Money Calculate(
        ServiceCategory category,
        decimal unitPrice,
        decimal quantity,
        decimal maximumWeightKilograms)
    {
        if (unitPrice <= 0)
        {
            throw new DomainRuleException("price.non_positive", "Harga layanan harus lebih dari nol.");
        }

        ValidateQuantity(category, quantity, maximumWeightKilograms);
        return new Money(quantity * unitPrice);
    }

    public static void ValidateQuantity(
        ServiceCategory category,
        decimal quantity,
        decimal maximumWeightKilograms)
    {
        if (category == ServiceCategory.PerKilo)
        {
            _ = new Weight(quantity, maximumWeightKilograms);
            return;
        }

        if (quantity < 1 || decimal.Truncate(quantity) != quantity)
        {
            throw new DomainRuleException("quantity.whole_number", "Jumlah layanan satuan harus berupa bilangan bulat minimal 1.");
        }
    }
}
