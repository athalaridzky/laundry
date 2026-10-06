using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Services;

public static class OrderWorkflow
{
    public static void EnsureTransition(OrderStatus current, OrderStatus next, PaymentStatus paymentStatus)
    {
        bool isValid = (current, next) switch
        {
            (OrderStatus.Received, OrderStatus.InProcess) => true,
            (OrderStatus.InProcess, OrderStatus.Done) => true,
            (OrderStatus.Done, OrderStatus.PickedUp) => paymentStatus == PaymentStatus.Paid,
            _ => false,
        };

        if (!isValid)
        {
            string message = next == OrderStatus.PickedUp && paymentStatus != PaymentStatus.Paid
                ? "Pesanan harus lunas sebelum diambil."
                : "Perubahan status pesanan tidak valid.";
            throw new DomainRuleException("order.invalid_transition", message);
        }
    }

    public static void EnsureCancellationAllowed(OrderStatus current, PaymentStatus paymentStatus, bool isOwner)
    {
        if (current is not (OrderStatus.Received or OrderStatus.InProcess))
        {
            throw new DomainRuleException("order.cancel_status", "Pesanan hanya dapat dibatalkan saat diterima atau diproses.");
        }

        if (paymentStatus != PaymentStatus.Unpaid && !isOwner)
        {
            throw new DomainRuleException("order.cancel_paid_owner", "Hanya Owner yang dapat membatalkan pesanan yang sudah dibayar.");
        }
    }
}
