namespace RefalinaLaundry.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(
        int? userId,
        string action,
        string entityName,
        string entityId,
        string? detail,
        DateTime createdAtUtc)
    {
        UserId = userId;
        Action = action.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId.Trim();
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
        CreatedAt = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
    }

    public long Id { get; private set; }

    public int? UserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? Detail { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
