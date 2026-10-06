using FluentAssertions;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Tests;

public sealed class OrderWorkflowTests
{
    [Fact]
    public void ChangeStatus_ReceivedDirectlyToPickedUp_Throws()
    {
        Order order = CreateOrder();

        Action action = () => order.ChangeStatus(OrderStatus.PickedUp, 1, DateTime.UtcNow);

        action.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void ChangeStatus_ToPickedUpWhenUnpaid_Throws()
    {
        Order order = CreateOrder();
        order.ChangeStatus(OrderStatus.InProcess, 1, DateTime.UtcNow);
        order.ChangeStatus(OrderStatus.Done, 1, DateTime.UtcNow);

        Action action = () => order.ChangeStatus(OrderStatus.PickedUp, 1, DateTime.UtcNow);

        action.Should().Throw<DomainRuleException>().WithMessage("*lunas*");
    }

    [Fact]
    public void ChangeStatus_ValidFlowAndPaid_Succeeds()
    {
        Order order = CreateOrder();
        order.AddPayment(order.TotalAmount, PaymentMethod.Cash, DateTime.UtcNow, 1, null);

        order.ChangeStatus(OrderStatus.InProcess, 1, DateTime.UtcNow);
        order.ChangeStatus(OrderStatus.Done, 1, DateTime.UtcNow);
        order.ChangeStatus(OrderStatus.PickedUp, 1, DateTime.UtcNow);

        order.Status.Should().Be(OrderStatus.PickedUp);
        order.StatusHistories.Should().HaveCount(4);
    }

    [Fact]
    public void Cancel_PaidOrderByCashier_Throws()
    {
        Order order = CreateOrder();
        order.AddPayment(10_000m, PaymentMethod.Cash, DateTime.UtcNow, 1, null);

        Action action = () => order.Cancel("Pelanggan meminta batal", false, 2, DateTime.UtcNow);

        action.Should().Throw<DomainRuleException>().WithMessage("*Owner*");
    }

    [Fact]
    public void Cancel_FromReceivedByOwner_RecordsReasonAndHistory()
    {
        Order order = CreateOrder();

        order.Cancel("Pelanggan salah layanan", true, 1, DateTime.UtcNow);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelReason.Should().Be("Pelanggan salah layanan");
        order.StatusHistories.Should().ContainSingle(history => history.ToStatus == OrderStatus.Cancelled);
    }

    private static Order CreateOrder()
    {
        DateTime now = DateTime.UtcNow;
        Service service = new("Reguler 3 Hari", ServiceCategory.PerKilo, 7000m, 3, true, now);
        OrderItem item = new(service, 2.5m, 100m);
        return Order.Create("RL-20261003-0001", 1, new[] { item }, now, now.AddDays(3), null, 1);
    }
}
