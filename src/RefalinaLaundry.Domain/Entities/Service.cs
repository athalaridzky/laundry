using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Entities;

public sealed class Service : AuditableEntity
{
    private Service()
    {
    }

    public Service(
        string name,
        ServiceCategory category,
        decimal unitPrice,
        int? durationDays,
        bool isActive,
        DateTime utcNow)
    {
        Apply(name, category, unitPrice, durationDays, isActive);
        SetCreated(utcNow);
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ServiceCategory Category { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int? DurationDays { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();

    public void Update(
        string name,
        ServiceCategory category,
        decimal unitPrice,
        int? durationDays,
        bool isActive,
        DateTime utcNow)
    {
        Apply(name, category, unitPrice, durationDays, isActive);
        Touch(utcNow);
    }

    public void SoftDelete(DateTime utcNow)
    {
        IsDeleted = true;
        IsActive = false;
        Touch(utcNow);
    }

    private void Apply(string name, ServiceCategory category, decimal unitPrice, int? durationDays, bool isActive)
    {
        string trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new DomainRuleException("service.name", "Nama layanan wajib diisi.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainRuleException("service.price", "Harga layanan harus lebih dari nol.");
        }

        if (durationDays < 0)
        {
            throw new DomainRuleException("service.duration", "Durasi layanan tidak boleh negatif.");
        }

        Name = trimmedName;
        Category = category;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        DurationDays = durationDays;
        IsActive = isActive;
    }
}
