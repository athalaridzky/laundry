namespace RefalinaLaundry.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    protected void SetCreated(DateTime utcNow)
    {
        CreatedAt = EnsureUtc(utcNow);
        UpdatedAt = CreatedAt;
    }

    protected void Touch(DateTime utcNow) => UpdatedAt = EnsureUtc(utcNow);

    protected static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
