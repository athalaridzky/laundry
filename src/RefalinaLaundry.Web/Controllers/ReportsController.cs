using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Owner)]
[Route("Reports")]
public sealed class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly IBusinessTimeZone _businessTime;
    private readonly IClock _clock;

    public ReportsController(IReportService reports, IBusinessTimeZone businessTime, IClock clock)
    {
        _reports = reports;
        _businessTime = businessTime;
        _clock = clock;
    }

    [HttpGet("")]
    [HttpGet("Daily")]
    [HttpGet("Monthly")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(_businessTime.ToLocal(_clock.UtcNow));
        DateOnly selectedFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        DateOnly selectedTo = to ?? today;
        ReportSummaryDto summary = await _reports.GetAsync(selectedFrom, selectedTo, cancellationToken);
        IReadOnlyList<ReportExportRowDto> rows = await _reports.GetRowsAsync(selectedFrom, selectedTo, cancellationToken);
        return View(new ReportPageViewModel(selectedFrom, selectedTo, summary, rows));
    }

    [HttpGet("Export")]
    public async Task<IActionResult> Export(
        DateOnly from,
        DateOnly to,
        string format = "xlsx",
        CancellationToken cancellationToken = default)
    {
        (byte[] content, string contentType, string fileName) =
            await _reports.ExportAsync(from, to, format, cancellationToken);
        return File(content, contentType, fileName);
    }
}
