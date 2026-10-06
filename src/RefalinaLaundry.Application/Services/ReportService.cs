using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reports;
    private readonly IReportExporter _exporter;
    private readonly IBusinessTimeZone _businessTime;

    public ReportService(
        IReportRepository reports,
        IReportExporter exporter,
        IBusinessTimeZone businessTime)
    {
        _reports = reports;
        _exporter = exporter;
        _businessTime = businessTime;
    }

    public async Task<ReportSummaryDto> GetAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        (DateTime fromUtc, DateTime toUtc) = GetUtcRange(fromDate, toDate);
        return await _reports.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<ReportExportRowDto>> GetRowsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        (DateTime fromUtc, DateTime toUtc) = GetUtcRange(fromDate, toDate);
        return await _reports.GetRowsAsync(fromUtc, toUtc, cancellationToken);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string format,
        CancellationToken cancellationToken = default)
    {
        (DateTime fromUtc, DateTime toUtc) = GetUtcRange(fromDate, toDate);
        ReportSummaryDto summary = await _reports.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        IReadOnlyList<ReportExportRowDto> rows = await _reports.GetRowsAsync(fromUtc, toUtc, cancellationToken);
        string normalizedFormat = format.Trim().ToLowerInvariant();
        string dateRange = $"{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}";

        return normalizedFormat switch
        {
            "xlsx" => (_exporter.ExportExcel(summary, rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"rekap-refalina-{dateRange}.xlsx"),
            "pdf" => (_exporter.ExportPdf(summary, rows), "application/pdf", $"rekap-refalina-{dateRange}.pdf"),
            _ => throw new ArgumentException("Format ekspor harus xlsx atau pdf.", nameof(format)),
        };
    }

    private (DateTime FromUtc, DateTime ToUtc) GetUtcRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentException("Tanggal akhir tidak boleh sebelum tanggal awal.", nameof(to));
        }

        DateTime fromLocal = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        DateTime toLocalExclusive = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (_businessTime.ToUtc(fromLocal), _businessTime.ToUtc(toLocalExclusive));
    }
}
