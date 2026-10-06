using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Entities;

public sealed class Payment
{
    private Payment()
    {
    }

    internal Payment(decimal amount, PaymentMethod method, DateTime paidAtUtc, int receivedByUserId, string? reference)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("payment.non_positive", "Jumlah pembayaran harus lebih dari nol.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Method = method;
        PaidAt = paidAtUtc.Kind == DateTimeKind.Utc ? paidAtUtc : DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
        ReceivedByUserId = receivedByUserId;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    public long Id { get; private set; }

    public long OrderId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public DateTime PaidAt { get; private set; }

    public int ReceivedByUserId { get; private set; }

    public string? Reference { get; private set; }

    public Order Order { get; private set; } = null!;
}
