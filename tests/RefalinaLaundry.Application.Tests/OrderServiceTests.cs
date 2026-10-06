using System.Data;
using FluentAssertions;
using NSubstitute;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Application.Services;
using RefalinaLaundry.Application.Validation;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Application.Tests;

public sealed class OrderServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 3, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_WithMixedItemsAndCashPayment_CreatesSnapshotAndChange()
    {
        TestContext context = CreateContext();
        Service regular = ServiceWithId(10, "Reguler 2 Hari", ServiceCategory.PerKilo, 8000m, 2);
        Service suit = ServiceWithId(11, "Jas", ServiceCategory.PerItem, 20_000m, null);
        Service bedcover = ServiceWithId(12, "Bedcover Besar", ServiceCategory.PerItem, 30_000m, null);
        context.Services.GetByIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { regular, suit, bedcover });
        Order? savedOrder = null;
        context.Orders.When(repository => repository.Add(Arg.Any<Order>()))
            .Do(call => savedOrder = call.Arg<Order>());
        CreateOrderCommand command = new(
            1,
            new[]
            {
                new CreateOrderItemCommand(10, 3.2m),
                new CreateOrderItemCommand(11, 1m),
                new CreateOrderItemCommand(12, 1m),
            },
            "Pisahkan pakaian putih",
            new InitialPaymentCommand(75_600m, PaymentMethod.Cash, 80_000m, null));

        CreateOrderResult result = await context.Service.CreateAsync(command, 7);

        result.OrderNumber.Should().Be("RL-20261003-0001");
        result.TotalAmount.Should().Be(75_600m);
        result.PaidAmount.Should().Be(75_600m);
        result.ChangeAmount.Should().Be(4_400m);
        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(3);
        savedOrder.EstimatedDoneAt.Should().Be(FixedNow.AddDays(2));
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenServiceMissing_ThrowsNotFound()
    {
        TestContext context = CreateContext();
        context.Services.GetByIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Service>());
        CreateOrderCommand command = new(1, new[] { new CreateOrderItemCommand(99, 1m) }, null, null);

        Func<Task> action = () => context.Service.CreateAsync(command, 7);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddPaymentAsync_PartialPayment_UpdatesStatus()
    {
        TestContext context = CreateContext();
        Service regular = ServiceWithId(10, "Reguler 3 Hari", ServiceCategory.PerKilo, 7000m, 3);
        Order order = Order.Create(
            "RL-20261003-0001",
            1,
            new[] { new OrderItem(regular, 2.5m, 100m) },
            FixedNow,
            FixedNow.AddDays(3),
            null,
            7);
        context.Orders.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        PaymentResult result = await context.Service.AddPaymentAsync(
            new AddPaymentCommand(1, 10_000m, PaymentMethod.Transfer, null, "TRX-01"),
            7);

        result.AppliedAmount.Should().Be(10_000m);
        result.PaymentStatus.Should().Be(PaymentStatus.PartiallyPaid);
        order.OutstandingAmount.Should().Be(7_500m);
    }

    private static TestContext CreateContext()
    {
        IOrderRepository orders = Substitute.For<IOrderRepository>();
        ICustomerRepository customers = Substitute.For<ICustomerRepository>();
        IServiceRepository services = Substitute.For<IServiceRepository>();
        IBusinessSettingRepository settings = Substitute.For<IBusinessSettingRepository>();
        IAuditLogRepository auditLogs = Substitute.For<IAuditLogRepository>();
        IOrderNumberGenerator numberGenerator = Substitute.For<IOrderNumberGenerator>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IClock clock = Substitute.For<IClock>();
        IBusinessTimeZone businessTime = Substitute.For<IBusinessTimeZone>();
        Customer customer = new("Siti Aminah", "081234567890", null, null, FixedNow);
        customers.GetAsync(1, Arg.Any<CancellationToken>()).Returns(customer);
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new BusinessSetting(
            "Refalina Laundry", "Jalan Material No 50", null, null, 80, "RL", 100m));
        numberGenerator.NextAsync("RL", new DateOnly(2026, 10, 3), Arg.Any<CancellationToken>())
            .Returns("RL-20261003-0001");
        unitOfWork.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(new TestTransaction());
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<CreateOrderResult>>>(),
                Arg.Any<IsolationLevel>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<CreateOrderResult>>>()(call.ArgAt<CancellationToken>(2)));
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<PaymentResult>>>(),
                Arg.Any<IsolationLevel>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<PaymentResult>>>()(call.ArgAt<CancellationToken>(2)));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        clock.UtcNow.Returns(FixedNow);
        businessTime.TimeZone.Returns(TimeZoneInfo.Utc);
        businessTime.ToLocal(Arg.Any<DateTime>()).Returns(call => call.Arg<DateTime>());
        businessTime.ToUtc(Arg.Any<DateTime>()).Returns(call => DateTime.SpecifyKind(call.Arg<DateTime>(), DateTimeKind.Utc));
        OrderService service = new(
            orders,
            customers,
            services,
            settings,
            auditLogs,
            numberGenerator,
            unitOfWork,
            clock,
            businessTime,
            new CreateOrderCommandValidator(),
            new AddPaymentCommandValidator());
        return new TestContext(service, orders, services, unitOfWork);
    }

    private static Service ServiceWithId(
        int id,
        string name,
        ServiceCategory category,
        decimal price,
        int? days)
    {
        Service service = new(name, category, price, days, true, FixedNow);
        typeof(Service).GetProperty(nameof(Service.Id))!.SetValue(service, id);
        return service;
    }

    private sealed record TestContext(
        OrderService Service,
        IOrderRepository Orders,
        IServiceRepository Services,
        IUnitOfWork UnitOfWork);

    private sealed class TestTransaction : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
