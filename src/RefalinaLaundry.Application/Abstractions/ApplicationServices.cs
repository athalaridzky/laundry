using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Abstractions;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<CustomerDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CustomerInput input, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, CustomerInput input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<ServiceDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceDto> CreateAsync(ServiceInput input, int userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, ServiceInput input, int userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<CreateOrderResult> CreateAsync(CreateOrderCommand command, int userId, CancellationToken cancellationToken = default);

    Task<OrderDetailDto> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<PaymentResult> AddPaymentAsync(AddPaymentCommand command, int userId, CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(ChangeOrderStatusCommand command, int userId, CancellationToken cancellationToken = default);

    Task CancelAsync(CancelOrderCommand command, int userId, bool isOwner, CancellationToken cancellationToken = default);

    Task MarkReceiptReprintedAsync(long id, int userId, CancellationToken cancellationToken = default);

    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ReportSummaryDto> GetAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportExportRowDto>> GetRowsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string format,
        CancellationToken cancellationToken = default);
}

public interface IBusinessSettingService
{
    Task<BusinessSettingDto> GetAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessSettingInput input, int userId, CancellationToken cancellationToken = default);
}
