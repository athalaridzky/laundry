using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IBusinessTimeZone
{
    TimeZoneInfo TimeZone { get; }

    DateTime ToLocal(DateTime utcDateTime);

    DateTime ToUtc(DateTime localDateTime);
}

public interface IReportExporter
{
    byte[] ExportExcel(ReportSummaryDto summary, IReadOnlyList<ReportExportRowDto> rows);

    byte[] ExportPdf(ReportSummaryDto summary, IReadOnlyList<ReportExportRowDto> rows);
}

public interface IUserAccountService
{
    Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
}
