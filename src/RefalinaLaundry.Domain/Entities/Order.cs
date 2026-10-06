using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Domain.Services;

namespace RefalinaLaundry.Domain.Entities;

public sealed class Order : AuditableEntity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<Payment> _payments = [];
    private readonly List<OrderStatusHistory> _statusHistories = [];

    private Order()
    {
    }

    private Order(
        string orderNumber,
        int customerId,
        IEnumerable<OrderItem> items,
        DateTime receivedAtUtc,
        DateTime? estimatedDoneAtUtc,
        string? notes,
        int createdByUserId)
    {
        OrderItem[] itemArray = items.ToArray();
        if (itemArray.Length == 0)
        {
            throw new DomainRuleException("order.items", "Transaksi harus memiliki minimal satu layanan.");
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainRuleException("order.number", "Nomor nota wajib diisi.");
        }

        OrderNumber = orderNumber.Trim();
        CustomerId = customerId;
        ReceivedAt = EnsureUtc(receivedAtUtc);
        EstimatedDoneAt = estimatedDoneAtUtc.HasValue ? EnsureUtc(estimatedDoneAtUtc.Value) : null;
        Status = OrderStatus.Received;
        PaymentStatus = PaymentStatus.Unpaid;
        TotalAmount = itemArray.Sum(item => item.Subtotal);
        PaidAmount = 0m;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedByUserId = createdByUserId;
        _items.AddRange(itemArray);
        _statusHistories.Add(new OrderStatusHistory(null, OrderStatus.Received, createdByUserId, ReceivedAt, "Pesanan diterima."));
        SetCreated(ReceivedAt);
    }

    public long Id { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public int CustomerId { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public DateTime? EstimatedDoneAt { get; private set; }

    public OrderStatus Status { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal PaidAmount { get; private set; }

    public string? Notes { get; private set; }

    public string? CancelReason { get; private set; }

    public int CreatedByUserId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public IReadOnlyCollection<OrderStatusHistory> StatusHistories => _statusHistories.AsReadOnly();

    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    public static Order Create(
        string orderNumber,
        int customerId,
        IEnumerable<OrderItem> items,
        DateTime receivedAtUtc,
        DateTime? estimatedDoneAtUtc,
        string? notes,
        int createdByUserId) =>
        new(orderNumber, customerId, items, receivedAtUtc, estimatedDoneAtUtc, notes, createdByUserId);

    public Payment AddPayment(
        decimal amount,
        PaymentMethod method,
        DateTime paidAtUtc,
        int receivedByUserId,
        string? reference)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainRuleException("payment.cancelled_order", "Pembayaran tidak dapat dicatat pada pesanan yang dibatalkan.");
        }

        decimal appliedAmount = PaymentRules.CalculateAppliedAmount(OutstandingAmount, amount, method, null);
        Payment payment = new(appliedAmount, method, paidAtUtc, receivedByUserId, reference);
        _payments.Add(payment);
        PaidAmount += appliedAmount;
        PaymentStatus = PaymentRules.GetStatus(TotalAmount, PaidAmount);
        Touch(paidAtUtc);
        return payment;
    }

    public void ChangeStatus(OrderStatus nextStatus, int changedByUserId, DateTime changedAtUtc, string? note = null)
    {
        OrderWorkflow.EnsureTransition(Status, nextStatus, PaymentStatus);
        OrderStatus previousStatus = Status;
        Status = nextStatus;
        _statusHistories.Add(new OrderStatusHistory(previousStatus, nextStatus, changedByUserId, changedAtUtc, note));
        Touch(changedAtUtc);
    }

    public void Cancel(string reason, bool isOwner, int changedByUserId, DateTime changedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("order.cancel_reason", "Alasan pembatalan wajib diisi.");
        }

        OrderWorkflow.EnsureCancellationAllowed(Status, PaymentStatus, isOwner);
        OrderStatus previousStatus = Status;
        Status = OrderStatus.Cancelled;
        CancelReason = reason.Trim();
        _statusHistories.Add(new OrderStatusHistory(previousStatus, OrderStatus.Cancelled, changedByUserId, changedAtUtc, CancelReason));
        Touch(changedAtUtc);
    }
}
