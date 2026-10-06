using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.ValueObjects;

namespace RefalinaLaundry.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    private Customer()
    {
    }

    public Customer(string name, string? phoneNumber, string? address, string? notes, DateTime utcNow)
    {
        Apply(name, phoneNumber, address, notes);
        SetCreated(utcNow);
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? Address { get; private set; }

    public string? Notes { get; private set; }

    public bool IsDeleted { get; private set; }

    public ICollection<Order> Orders { get; } = new List<Order>();

    public void Update(string name, string? phoneNumber, string? address, string? notes, DateTime utcNow)
    {
        Apply(name, phoneNumber, address, notes);
        Touch(utcNow);
    }

    public void SoftDelete(DateTime utcNow)
    {
        IsDeleted = true;
        Touch(utcNow);
    }

    private void Apply(string name, string? phoneNumber, string? address, string? notes)
    {
        string trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new DomainRuleException("customer.name", "Nama pelanggan wajib diisi.");
        }

        Name = trimmedName;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : new PhoneNumber(phoneNumber).Value;
        Address = NormalizeOptional(address);
        Notes = NormalizeOptional(notes);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
