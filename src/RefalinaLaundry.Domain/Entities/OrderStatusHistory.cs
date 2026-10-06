using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Domain.Entities;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        int changedByUserId,
        DateTime changedAtUtc,
        string? note)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAtUtc.Kind == DateTimeKind.Utc
            ? changedAtUtc
            : DateTime.SpecifyKind(changedAtUtc, DateTimeKind.Utc);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public long Id { get; private set; }

    public long OrderId { get; private set; }

    public OrderStatus? FromStatus { get; private set; }

    public OrderStatus ToStatus { get; private set; }

    public int ChangedByUserId { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string? Note { get; private set; }

    public Order Order { get; private set; } = null!;
}
