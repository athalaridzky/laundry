using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Services;

namespace RefalinaLaundry.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(Service service, decimal quantity, decimal maximumWeightKilograms)
    {
        ArgumentNullException.ThrowIfNull(service);

        ServiceId = service.Id;
        Service = service;
        ServiceNameSnapshot = service.Name;
        UnitPriceSnapshot = service.UnitPrice;
        Quantity = quantity;
        Subtotal = PricingCalculator.Calculate(
            service.Category,
            service.UnitPrice,
            quantity,
            maximumWeightKilograms).Amount;
    }

    public long Id { get; private set; }

    public long OrderId { get; private set; }

    public int ServiceId { get; private set; }

    public string ServiceNameSnapshot { get; private set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal Subtotal { get; private set; }

    public Order Order { get; private set; } = null!;

    public Service Service { get; private set; } = null!;
}
