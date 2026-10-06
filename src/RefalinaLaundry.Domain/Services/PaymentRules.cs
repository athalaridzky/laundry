using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Services;

public static class PaymentRules
{
    public static PaymentStatus GetStatus(decimal totalAmount, decimal paidAmount)
    {
        if (totalAmount <= 0)
        {
            throw new DomainRuleException("payment.total", "Total transaksi harus lebih dari nol.");
        }

        if (paidAmount < 0 || paidAmount > totalAmount)
        {
            throw new DomainRuleException("payment.exceeds_total", "Pembayaran tidak boleh melebihi total tagihan.");
        }

        if (paidAmount == 0)
        {
            return PaymentStatus.Unpaid;
        }

        return paidAmount < totalAmount ? PaymentStatus.PartiallyPaid : PaymentStatus.Paid;
    }

    public static decimal CalculateAppliedAmount(
        decimal outstandingAmount,
        decimal requestedAmount,
        PaymentMethod method,
        decimal? cashTendered)
    {
        if (outstandingAmount <= 0)
        {
            throw new DomainRuleException("payment.settled", "Pesanan sudah lunas.");
        }

        if (requestedAmount <= 0)
        {
            throw new DomainRuleException("payment.non_positive", "Jumlah pembayaran harus lebih dari nol.");
        }

        if (method != PaymentMethod.Cash && requestedAmount > outstandingAmount)
        {
            throw new DomainRuleException("payment.overpayment", "Pembayaran non-tunai tidak boleh melebihi sisa tagihan.");
        }

        if (method == PaymentMethod.Cash && cashTendered.HasValue)
        {
            if (cashTendered.Value < requestedAmount)
            {
                throw new DomainRuleException("payment.cash_insufficient", "Uang diterima kurang dari jumlah pembayaran.");
            }

            return decimal.Min(requestedAmount, outstandingAmount);
        }

        if (requestedAmount > outstandingAmount)
        {
            throw new DomainRuleException("payment.overpayment", "Pembayaran tidak boleh melebihi sisa tagihan.");
        }

        return requestedAmount;
    }
}
