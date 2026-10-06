using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.ValueObjects;

public readonly record struct PhoneNumber
{
    public PhoneNumber(string value)
    {
        string digits = new(value.Where(char.IsDigit).ToArray());
        string normalized = digits switch
        {
            _ when digits.StartsWith('0') => $"62{digits[1..]}",
            _ when digits.StartsWith('8') => $"62{digits}",
            _ when digits.StartsWith("62", StringComparison.Ordinal) => digits,
            _ => throw new DomainRuleException("phone.format", "Nomor HP harus diawali 08, 8, atau 62."),
        };

        if (normalized.Length is < 10 or > 15)
        {
            throw new DomainRuleException("phone.length", "Nomor HP harus terdiri dari 10 sampai 15 digit.");
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
