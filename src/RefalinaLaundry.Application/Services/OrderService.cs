using System.Data;
using FluentValidation;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Services;

namespace RefalinaLaundry.Application.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly IBusinessSettingRepository _settings;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IOrderNumberGenerator _numberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IBusinessTimeZone _businessTime;
    private readonly IValidator<CreateOrderCommand> _createValidator;
    private readonly IValidator<AddPaymentCommand> _paymentValidator;

    public OrderService(
        IOrderRepository orders,
        ICustomerRepository customers,
        IServiceRepository services,
        IBusinessSettingRepository settings,
        IAuditLogRepository auditLogs,
        IOrderNumberGenerator numberGenerator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IBusinessTimeZone businessTime,
        IValidator<CreateOrderCommand> createValidator,
        IValidator<AddPaymentCommand> paymentValidator)
    {
        _orders = orders;
        _customers = customers;
        _services = services;
        _settings = settings;
        _auditLogs = auditLogs;
        _numberGenerator = numberGenerator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _businessTime = businessTime;
        _createValidator = createValidator;
        _paymentValidator = paymentValidator;
    }

    public async Task<CreateOrderResult> CreateAsync(
        CreateOrderCommand command,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);
        _ = await _customers.GetAsync(command.CustomerId, cancellationToken)
            ?? throw new EntityNotFoundException("Pelanggan tidak ditemukan.");
        BusinessSetting setting = await GetSettingsAsync(cancellationToken);
        IReadOnlyList<Service> services = await LoadServicesAsync(command.Items, cancellationToken);
        DateTime receivedAtUtc = _clock.UtcNow;
        DateTime? estimatedDoneAtUtc = OrderEstimateCalculator.CalculateUtc(
            receivedAtUtc,
            services.Select(service => service.DurationDays),
            _businessTime.TimeZone);
        List<OrderItem> items = BuildItems(command.Items, services, setting.MaxWeightKg);

        return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            DateOnly businessDate = DateOnly.FromDateTime(_businessTime.ToLocal(receivedAtUtc));
            string orderNumber = await _numberGenerator.NextAsync(
                setting.OrderNumberPrefix,
                businessDate,
                transactionCancellationToken);
            Order order = Order.Create(
                orderNumber,
                command.CustomerId,
                items,
                receivedAtUtc,
                estimatedDoneAtUtc,
                command.Notes,
                userId);

            decimal change = ApplyInitialPayment(order, command.InitialPayment, userId, receivedAtUtc);
            _orders.Add(order);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return new CreateOrderResult(order.Id, order.OrderNumber, order.TotalAmount, order.PaidAmount, change);
        }, IsolationLevel.Serializable, cancellationToken);
    }

    public async Task<OrderDetailDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        await _orders.GetDetailAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("Pesanan tidak ditemukan.");

    public Task<PagedResult<OrderListItemDto>> SearchAsync(
        OrderSearchCriteria criteria,
        CancellationToken cancellationToken = default) =>
        _orders.SearchAsync(criteria with
        {
            Page = Math.Max(1, criteria.Page),
            PageSize = Math.Clamp(criteria.PageSize, 10, 100),
        }, _clock.UtcNow, cancellationToken);

    public async Task<PaymentResult> AddPaymentAsync(
        AddPaymentCommand command,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _paymentValidator.ValidateAndThrowAsync(command, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            Order order = await FindForUpdateAsync(command.OrderId, transactionCancellationToken);
            decimal appliedAmount = PaymentRules.CalculateAppliedAmount(
                order.OutstandingAmount,
                command.Amount,
                command.Method,
                command.CashTendered);
            order.AddPayment(appliedAmount, command.Method, _clock.UtcNow, userId, command.Reference);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

            decimal change = command.Method == PaymentMethod.Cash && command.CashTendered.HasValue
                ? command.CashTendered.Value - appliedAmount
                : 0m;
            return new PaymentResult(appliedAmount, change, order.PaymentStatus);
        }, IsolationLevel.Serializable, cancellationToken);
    }

    public async Task ChangeStatusAsync(
        ChangeOrderStatusCommand command,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Order order = await FindForUpdateAsync(command.OrderId, cancellationToken);
        order.ChangeStatus(command.Status, userId, _clock.UtcNow, command.Note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(
        CancelOrderCommand command,
        int userId,
        bool isOwner,
        CancellationToken cancellationToken = default)
    {
        Order order = await FindForUpdateAsync(command.OrderId, cancellationToken);
        order.Cancel(command.Reason, isOwner, userId, _clock.UtcNow);
        _auditLogs.Add(new AuditLog(
            userId,
            AuditActions.OrderCancelled,
            nameof(Order),
            order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.Reason,
            _clock.UtcNow));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReceiptReprintedAsync(long id, int userId, CancellationToken cancellationToken = default)
    {
        _ = await FindForUpdateAsync(id, cancellationToken);
        _auditLogs.Add(new AuditLog(
            userId,
            AuditActions.ReceiptReprinted,
            nameof(Order),
            id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Struk dicetak ulang sebagai SALINAN.",
            _clock.UtcNow));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        DateTime localNow = _businessTime.ToLocal(_clock.UtcNow);
        DateTime fromUtc = _businessTime.ToUtc(DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified));
        DateTime toUtc = _businessTime.ToUtc(DateTime.SpecifyKind(localNow.Date.AddDays(1), DateTimeKind.Unspecified));
        return await _orders.GetDashboardAsync(fromUtc, toUtc, _clock.UtcNow, cancellationToken);
    }

    private async Task<IReadOnlyList<Service>> LoadServicesAsync(
        IReadOnlyList<CreateOrderItemCommand> requestedItems,
        CancellationToken cancellationToken)
    {
        int[] ids = requestedItems.Select(item => item.ServiceId).Distinct().ToArray();
        IReadOnlyList<Service> services = await _services.GetByIdsAsync(ids, cancellationToken);
        if (services.Count != ids.Length || services.Any(service => !service.IsActive || service.IsDeleted))
        {
            throw new EntityNotFoundException("Satu atau lebih layanan tidak tersedia.");
        }

        return services;
    }

    private static List<OrderItem> BuildItems(
        IReadOnlyList<CreateOrderItemCommand> requestedItems,
        IReadOnlyList<Service> services,
        decimal maxWeightKg)
    {
        Dictionary<int, Service> byId = services.ToDictionary(service => service.Id);
        return requestedItems
            .Select(item => new OrderItem(byId[item.ServiceId], item.Quantity, maxWeightKg))
            .ToList();
    }

    private static decimal ApplyInitialPayment(Order order, InitialPaymentCommand? payment, int userId, DateTime paidAtUtc)
    {
        if (payment is null)
        {
            return 0m;
        }

        decimal appliedAmount = PaymentRules.CalculateAppliedAmount(
            order.TotalAmount,
            payment.Amount,
            payment.Method,
            payment.CashTendered);
        order.AddPayment(appliedAmount, payment.Method, paidAtUtc, userId, payment.Reference);
        return payment.Method == PaymentMethod.Cash && payment.CashTendered.HasValue
            ? payment.CashTendered.Value - appliedAmount
            : 0m;
    }

    private async Task<Order> FindForUpdateAsync(long id, CancellationToken cancellationToken) =>
        await _orders.GetForUpdateAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("Pesanan tidak ditemukan.");

    private async Task<BusinessSetting> GetSettingsAsync(CancellationToken cancellationToken) =>
        await _settings.GetAsync(cancellationToken)
        ?? throw new EntityNotFoundException("Pengaturan usaha belum tersedia.");
}
