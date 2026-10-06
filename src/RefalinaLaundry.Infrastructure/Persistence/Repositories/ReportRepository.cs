using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly AppDbContext _dbContext;

    public ReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.Payment> payments = _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PaidAt >= fromUtc && payment.PaidAt < toUtc);
        decimal totalRevenue = await payments.SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        int transactionCount = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(
                order => order.ReceivedAt >= fromUtc &&
                         order.ReceivedAt < toUtc &&
                         order.Status != OrderStatus.Cancelled,
                cancellationToken);
        var methodAggregates = await payments
            .GroupBy(payment => payment.Method)
            .Select(group => new
            {
                Method = group.Key,
                Amount = group.Sum(payment => payment.Amount),
                Count = group.Count(),
            })
            .ToListAsync(cancellationToken);
        List<PaymentMethodTotalDto> byMethod = methodAggregates
            .OrderBy(item => item.Method)
            .Select(item => new PaymentMethodTotalDto(item.Method, item.Amount, item.Count))
            .ToList();

        decimal totalWeight = await (
            from item in _dbContext.OrderItems.AsNoTracking()
            join service in _dbContext.Services.IgnoreQueryFilters().AsNoTracking() on item.ServiceId equals service.Id
            join order in _dbContext.Orders.AsNoTracking() on item.OrderId equals order.Id
            where service.Category == ServiceCategory.PerKilo &&
                  order.ReceivedAt >= fromUtc && order.ReceivedAt < toUtc &&
                  order.Status != OrderStatus.Cancelled
            select (decimal?)item.Quantity)
            .SumAsync(cancellationToken) ?? 0m;

        return new ReportSummaryDto(fromUtc, toUtc, totalRevenue, transactionCount, totalWeight, byMethod);
    }

    public async Task<IReadOnlyList<ReportExportRowDto>> GetRowsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) =>
        await (
            from payment in _dbContext.Payments.AsNoTracking()
            join order in _dbContext.Orders.IgnoreQueryFilters().AsNoTracking() on payment.OrderId equals order.Id
            join customer in _dbContext.Customers.IgnoreQueryFilters().AsNoTracking() on order.CustomerId equals customer.Id
            join user in _dbContext.Users.AsNoTracking() on payment.ReceivedByUserId equals user.Id
            where payment.PaidAt >= fromUtc && payment.PaidAt < toUtc
            orderby payment.PaidAt
            select new ReportExportRowDto(
                payment.PaidAt,
                order.OrderNumber,
                customer.Name,
                payment.Method,
                payment.Amount,
                user.FullName))
            .ToListAsync(cancellationToken);
}
