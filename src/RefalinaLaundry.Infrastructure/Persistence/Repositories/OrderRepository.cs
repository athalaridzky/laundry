using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Infrastructure.Identity;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .Include(order => order.Payments)
            .Include(order => order.StatusHistories)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<OrderDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        Order? order = await _dbContext.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.StatusHistories)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        int[] serviceIds = order.Items.Select(item => item.ServiceId).Distinct().ToArray();
        Dictionary<int, ServiceCategory> categories = await _dbContext.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(service => serviceIds.Contains(service.Id))
            .ToDictionaryAsync(service => service.Id, service => service.Category, cancellationToken);

        int[] userIds = order.Payments.Select(payment => payment.ReceivedByUserId)
            .Concat(order.StatusHistories.Select(history => history.ChangedByUserId))
            .Append(order.CreatedByUserId)
            .Distinct()
            .ToArray();
        Dictionary<int, string> users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => DisplayName(user), cancellationToken);

        CustomerDto customer = new(
            order.CustomerId,
            order.Customer.Name,
            order.Customer.PhoneNumber,
            order.Customer.Address,
            order.Customer.Notes);
        List<OrderItemDto> items = order.Items
            .OrderBy(item => item.Id)
            .Select(item => new OrderItemDto(
                item.Id,
                item.ServiceNameSnapshot,
                categories.GetValueOrDefault(item.ServiceId),
                item.UnitPriceSnapshot,
                item.Quantity,
                item.Subtotal))
            .ToList();
        List<PaymentDto> payments = order.Payments
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => new PaymentDto(
                payment.Id,
                payment.Amount,
                payment.Method,
                payment.PaidAt,
                users.GetValueOrDefault(payment.ReceivedByUserId, "Pengguna"),
                payment.Reference))
            .ToList();
        List<OrderStatusHistoryDto> histories = order.StatusHistories
            .OrderBy(history => history.ChangedAt)
            .Select(history => new OrderStatusHistoryDto(
                history.Id,
                history.FromStatus,
                history.ToStatus,
                history.ChangedAt,
                users.GetValueOrDefault(history.ChangedByUserId, "Pengguna"),
                history.Note))
            .ToList();

        return new OrderDetailDto(
            order.Id,
            order.OrderNumber,
            customer,
            order.ReceivedAt,
            order.EstimatedDoneAt,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.PaidAmount,
            order.Notes,
            order.CancelReason,
            users.GetValueOrDefault(order.CreatedByUserId, "Pengguna"),
            items,
            payments,
            histories);
    }

    public async Task<PagedResult<OrderListItemDto>> SearchAsync(
        OrderSearchCriteria criteria,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _dbContext.Orders.IgnoreQueryFilters().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            string term = criteria.Search.Trim();
            string phoneTerm = NormalizePhoneSearch(term);
            query = query.Where(order =>
                order.OrderNumber.StartsWith(term) ||
                order.Customer.Name.StartsWith(term) ||
                (order.Customer.PhoneNumber != null && order.Customer.PhoneNumber.StartsWith(phoneTerm)));
        }

        if (criteria.FromUtc.HasValue)
        {
            query = query.Where(order => order.ReceivedAt >= criteria.FromUtc.Value);
        }

        if (criteria.ToUtc.HasValue)
        {
            query = query.Where(order => order.ReceivedAt < criteria.ToUtc.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(order => order.Status == criteria.Status.Value);
        }

        if (criteria.PaymentStatus.HasValue)
        {
            query = query.Where(order => order.PaymentStatus == criteria.PaymentStatus.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        List<OrderListItemDto> items = await query
            .OrderByDescending(order => order.ReceivedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(order => new OrderListItemDto(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.Customer.PhoneNumber,
                order.ReceivedAt,
                order.EstimatedDoneAt,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount,
                order.PaidAmount,
                order.EstimatedDoneAt.HasValue &&
                order.EstimatedDoneAt.Value < utcNow &&
                (order.Status == OrderStatus.Received || order.Status == OrderStatus.InProcess)))
            .ToListAsync(cancellationToken);
        return new PagedResult<OrderListItemDto>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<DashboardDto> GetDashboardAsync(
        DateTime fromUtc,
        DateTime toUtc,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        int ordersToday = await _dbContext.Orders.CountAsync(
            order => order.ReceivedAt >= fromUtc && order.ReceivedAt < toUtc,
            cancellationToken);
        int unpaidOrders = await _dbContext.Orders.CountAsync(
            order => order.PaymentStatus != PaymentStatus.Paid && order.Status != OrderStatus.Cancelled,
            cancellationToken);
        int readyForPickup = await _dbContext.Orders.CountAsync(
            order => order.Status == OrderStatus.Done,
            cancellationToken);
        int overdueOrders = await _dbContext.Orders.CountAsync(
            order => order.EstimatedDoneAt.HasValue &&
                     order.EstimatedDoneAt < utcNow &&
                     (order.Status == OrderStatus.Received || order.Status == OrderStatus.InProcess),
            cancellationToken);
        decimal revenueToday = await _dbContext.Payments
            .Where(payment => payment.PaidAt >= fromUtc && payment.PaidAt < toUtc)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        PagedResult<OrderListItemDto> recent = await SearchAsync(
            new OrderSearchCriteria(null, null, null, null, null, 1, 8),
            utcNow,
            cancellationToken);
        return new DashboardDto(
            ordersToday,
            unpaidOrders,
            readyForPickup,
            overdueOrders,
            revenueToday,
            recent.Items);
    }

    public void Add(Order order) => _dbContext.Orders.Add(order);

    private static string DisplayName(AppUser user) =>
        string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Pengguna" : user.FullName;

    private static string NormalizePhoneSearch(string value)
    {
        string digits = new(value.Where(char.IsDigit).ToArray());
        return digits.StartsWith('0') ? $"62{digits[1..]}" : digits;
    }
}
