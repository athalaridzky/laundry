using FluentAssertions;
using NSubstitute;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Application.Services;

namespace RefalinaLaundry.Application.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task ExportAsync_Xlsx_UsesExactLocalDateRange()
    {
        IReportRepository repository = Substitute.For<IReportRepository>();
        IReportExporter exporter = Substitute.For<IReportExporter>();
        IBusinessTimeZone timeZone = Substitute.For<IBusinessTimeZone>();
        timeZone.ToUtc(Arg.Any<DateTime>()).Returns(call => DateTime.SpecifyKind(call.Arg<DateTime>(), DateTimeKind.Utc));
        repository.GetSummaryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => new ReportSummaryDto(call.ArgAt<DateTime>(0), call.ArgAt<DateTime>(1), 10_000m, 1, 2.5m, []));
        repository.GetRowsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReportExportRowDto>());
        exporter.ExportExcel(Arg.Any<ReportSummaryDto>(), Arg.Any<IReadOnlyList<ReportExportRowDto>>())
            .Returns(new byte[] { 1, 2, 3 });
        ReportService service = new(repository, exporter, timeZone);

        (byte[] content, string contentType, string fileName) = await service.ExportAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 3),
            "xlsx");

        content.Should().Equal(1, 2, 3);
        contentType.Should().Contain("spreadsheet");
        fileName.Should().Be("rekap-refalina-20261001-20261003.xlsx");
        await repository.Received(1).GetSummaryAsync(
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }
}
