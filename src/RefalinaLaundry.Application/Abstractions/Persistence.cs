using System.Data;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(string phoneNumber, int? excludingId, CancellationToken cancellationToken = default);

    void Add(Customer customer);
}

public interface IServiceRepository
{
    Task<Service?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Service>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken = default);

    void Add(Service service);
}

public interface IOrderRepository
{
    Task<Order?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchCriteria criteria, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<DashboardDto> GetDashboardAsync(DateTime fromUtc, DateTime toUtc, DateTime utcNow, CancellationToken cancellationToken = default);

    void Add(Order order);
}

public interface IBusinessSettingRepository
{
    Task<BusinessSetting?> GetAsync(CancellationToken cancellationToken = default);

    void Add(BusinessSetting setting);
}

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
}

public interface IReportRepository
{
    Task<ReportSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportExportRowDto>> GetRowsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

public interface IOrderNumberGenerator
{
    Task<string> NextAsync(string prefix, DateOnly businessDate, CancellationToken cancellationToken = default);
}

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);

    Task<IAppTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
