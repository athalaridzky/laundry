using System.Globalization;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Web.Common;

public static class UiFormat
{
    private static readonly CultureInfo IndonesianCulture = CultureInfo.GetCultureInfo("id-ID");

    public static string Currency(decimal amount) => string.Format(IndonesianCulture, "Rp {0:N0}", amount);

    public static string Weight(decimal kilograms) => $"{kilograms.ToString("0.#", IndonesianCulture)} kg";

    public static string OrderStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Received => "Diterima",
        OrderStatus.InProcess => "Diproses",
        OrderStatus.Done => "Selesai",
        OrderStatus.PickedUp => "Diambil",
        OrderStatus.Cancelled => "Dibatalkan",
        _ => status.ToString(),
    };

    public static string PaymentStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Belum lunas",
        PaymentStatus.PartiallyPaid => "Dibayar sebagian",
        PaymentStatus.Paid => "Lunas",
        _ => status.ToString(),
    };

    public static string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tunai",
        PaymentMethod.Transfer => "Transfer",
        PaymentMethod.Qris => "QRIS",
        _ => method.ToString(),
    };

    public static string StatusCss(OrderStatus status) => status switch
    {
        OrderStatus.Done or OrderStatus.PickedUp => "status-success",
        OrderStatus.Cancelled => "status-danger",
        OrderStatus.InProcess => "status-info",
        _ => "status-neutral",
    };

    public static string PaymentCss(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "status-success",
        PaymentStatus.PartiallyPaid => "status-warning",
        _ => "status-danger",
    };
}
