using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Application.Contracts;

public sealed record PaymentMethodTotalDto(PaymentMethod Method, decimal Amount, int Count);

public sealed record ReportSummaryDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal TotalRevenue,
    int TransactionCount,
    decimal TotalWeightKg,
    IReadOnlyList<PaymentMethodTotalDto> ByMethod);

public sealed record ReportExportRowDto(
    DateTime PaidAtUtc,
    string OrderNumber,
    string CustomerName,
    PaymentMethod Method,
    decimal Amount,
    string ReceivedBy);

public sealed record DashboardDto(
    int OrdersToday,
    int UnpaidOrders,
    int ReadyForPickup,
    int OverdueOrders,
    decimal RevenueToday,
    IReadOnlyList<OrderListItemDto> RecentOrders);
