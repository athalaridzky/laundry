using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Application.Contracts;

public sealed record CreateOrderItemCommand(int ServiceId, decimal Quantity);

public sealed record InitialPaymentCommand(
    decimal Amount,
    PaymentMethod Method,
    decimal? CashTendered,
    string? Reference);

public sealed record CreateOrderCommand(
    int CustomerId,
    IReadOnlyList<CreateOrderItemCommand> Items,
    string? Notes,
    InitialPaymentCommand? InitialPayment);

public sealed record CreateOrderResult(
    long Id,
    string OrderNumber,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount);

public sealed record AddPaymentCommand(
    long OrderId,
    decimal Amount,
    PaymentMethod Method,
    decimal? CashTendered,
    string? Reference);

public sealed record PaymentResult(decimal AppliedAmount, decimal ChangeAmount, PaymentStatus PaymentStatus);

public sealed record ChangeOrderStatusCommand(long OrderId, OrderStatus Status, string? Note);

public sealed record CancelOrderCommand(long OrderId, string Reason);

public sealed record OrderSearchCriteria(
    string? Search,
    DateTime? FromUtc,
    DateTime? ToUtc,
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    int Page = 1,
    int PageSize = 20);

public sealed record OrderListItemDto(
    long Id,
    string OrderNumber,
    string CustomerName,
    string? CustomerPhone,
    DateTime ReceivedAtUtc,
    DateTime? EstimatedDoneAtUtc,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    decimal PaidAmount,
    bool IsOverdue);

public sealed record OrderItemDto(
    long Id,
    string ServiceName,
    ServiceCategory Category,
    decimal UnitPrice,
    decimal Quantity,
    decimal Subtotal);

public sealed record PaymentDto(
    long Id,
    decimal Amount,
    PaymentMethod Method,
    DateTime PaidAtUtc,
    string ReceivedBy,
    string? Reference);

public sealed record OrderStatusHistoryDto(
    long Id,
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    DateTime ChangedAtUtc,
    string ChangedBy,
    string? Note);

public sealed record OrderDetailDto(
    long Id,
    string OrderNumber,
    CustomerDto Customer,
    DateTime ReceivedAtUtc,
    DateTime? EstimatedDoneAtUtc,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    decimal PaidAmount,
    string? Notes,
    string? CancelReason,
    string CreatedBy,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistories)
{
    public decimal OutstandingAmount => TotalAmount - PaidAmount;
}

public sealed record OrderPageDto(PagedResult<OrderListItemDto> Orders, OrderSearchCriteria Filter);
