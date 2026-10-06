using System.ComponentModel.DataAnnotations;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Web.Models;

public sealed class OrderItemInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Layanan wajib dipilih.")]
    public int ServiceId { get; set; }

    [Range(typeof(decimal), "0.1", "999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Berat atau jumlah harus lebih dari nol.")]
    public decimal Quantity { get; set; }
}

public sealed class CreateOrderInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Pelanggan wajib dipilih.")]
    public int CustomerId { get; set; }

    public string CustomerDisplay { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<OrderItemInputModel> Items { get; set; } = [new()];

    [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Jumlah pembayaran tidak valid.")]
    public decimal PaymentAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Uang diterima tidak valid.")]
    public decimal? CashTendered { get; set; }

    [StringLength(100)]
    public string? PaymentReference { get; set; }

    public CreateOrderCommand ToCommand()
    {
        List<CreateOrderItemCommand> items = Items
            .Where(item => item.ServiceId > 0 && item.Quantity > 0)
            .Select(item => new CreateOrderItemCommand(item.ServiceId, item.Quantity))
            .ToList();
        InitialPaymentCommand? payment = PaymentAmount > 0
            ? new InitialPaymentCommand(PaymentAmount, PaymentMethod, CashTendered, PaymentReference)
            : null;
        return new CreateOrderCommand(CustomerId, items, Notes, payment);
    }
}

public sealed record CreateOrderPageViewModel(CreateOrderInputModel Form, IReadOnlyList<ServiceDto> Services);

public sealed class PaymentFormModel
{
    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Jumlah pembayaran harus lebih dari nol.")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public decimal? CashTendered { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }
}

public sealed record OrderIndexViewModel(
    PagedResult<OrderListItemDto> Orders,
    string? Search,
    DateOnly? From,
    DateOnly? To,
    OrderStatus? Status,
    PaymentStatus? PaymentStatus);

public sealed record OrderDetailPageViewModel(OrderDetailDto Order, PaymentFormModel Payment);

public sealed record ReceiptViewModel(OrderDetailDto Order, BusinessSettingDto Settings, bool IsCopy, decimal ChangeAmount);
