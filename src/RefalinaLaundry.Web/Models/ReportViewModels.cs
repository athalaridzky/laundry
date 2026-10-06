using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Web.Models;

public sealed record ReportPageViewModel(
    DateOnly From,
    DateOnly To,
    ReportSummaryDto Summary,
    IReadOnlyList<ReportExportRowDto> Rows);
