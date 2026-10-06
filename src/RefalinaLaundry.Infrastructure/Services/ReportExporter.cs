using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Infrastructure.Services;

public sealed class ReportExporter : IReportExporter
{
    private static readonly CultureInfo IndonesianCulture = CultureInfo.GetCultureInfo("id-ID");
    private readonly IBusinessTimeZone _businessTime;

    public ReportExporter(IBusinessTimeZone businessTime)
    {
        _businessTime = businessTime;
    }

    public byte[] ExportExcel(ReportSummaryDto summary, IReadOnlyList<ReportExportRowDto> rows)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Rekap Pembayaran");
        worksheet.Cell("A1").Value = "Rekap Pembayaran Refalina Laundry";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;
        worksheet.Range("A1:F1").Merge();
        worksheet.Cell("A2").Value = $"Periode UTC: {summary.FromUtc:dd MMM yyyy} - {summary.ToUtc.AddDays(-1):dd MMM yyyy}";
        worksheet.Range("A2:F2").Merge();
        worksheet.Cell("A4").Value = "Total pendapatan";
        worksheet.Cell("B4").Value = summary.TotalRevenue;
        worksheet.Cell("B4").Style.NumberFormat.Format = "[$Rp-421] #,##0";
        worksheet.Cell("A5").Value = "Jumlah transaksi";
        worksheet.Cell("B5").Value = summary.TransactionCount;
        worksheet.Cell("A6").Value = "Total berat";
        worksheet.Cell("B6").Value = summary.TotalWeightKg;
        worksheet.Cell("B6").Style.NumberFormat.Format = "0.0 \"kg\"";

        string[] headers = ["Tanggal", "No. Nota", "Pelanggan", "Metode", "Jumlah", "Kasir"];
        for (int index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(8, index + 1).Value = headers[index];
        }

        worksheet.Range("A8:F8").Style.Font.Bold = true;
        worksheet.Range("A8:F8").Style.Fill.BackgroundColor = XLColor.FromHtml("#DDECE8");
        int rowNumber = 9;
        foreach (ReportExportRowDto row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = _businessTime.ToLocal(row.PaidAtUtc);
            worksheet.Cell(rowNumber, 1).Style.DateFormat.Format = "dd mmm yyyy hh:mm";
            worksheet.Cell(rowNumber, 2).Value = row.OrderNumber;
            worksheet.Cell(rowNumber, 3).Value = row.CustomerName;
            worksheet.Cell(rowNumber, 4).Value = row.Method.ToString();
            worksheet.Cell(rowNumber, 5).Value = row.Amount;
            worksheet.Cell(rowNumber, 5).Style.NumberFormat.Format = "[$Rp-421] #,##0";
            worksheet.Cell(rowNumber, 6).Value = row.ReceivedBy;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(8);
        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportPdf(ReportSummaryDto summary, IReadOnlyList<ReportExportRowDto> rows) =>
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(9));
                page.Header().Column(column =>
                {
                    column.Item().Text("Rekap Pembayaran Refalina Laundry").SemiBold().FontSize(16);
                    column.Item().Text($"Total {FormatCurrency(summary.TotalRevenue)} · {summary.TransactionCount} transaksi · {summary.TotalWeightKg:0.#} kg");
                });
                page.Content().PaddingVertical(16).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(105);
                        columns.RelativeColumn();
                        columns.ConstantColumn(65);
                        columns.ConstantColumn(80);
                    });
                    table.Header(header =>
                    {
                        AddHeader(header, "Tanggal");
                        AddHeader(header, "No. Nota");
                        AddHeader(header, "Pelanggan");
                        AddHeader(header, "Metode");
                        AddHeader(header, "Jumlah");
                    });
                    foreach (ReportExportRowDto row in rows)
                    {
                        AddCell(table, _businessTime.ToLocal(row.PaidAtUtc).ToString("dd MMM HH:mm", IndonesianCulture));
                        AddCell(table, row.OrderNumber);
                        AddCell(table, row.CustomerName);
                        AddCell(table, row.Method.ToString());
                        AddCell(table, FormatCurrency(row.Amount));
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Halaman ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

    private static void AddHeader(TableCellDescriptor table, string text) =>
        table.Cell().Background("#DDECE8").Padding(5).Text(text).SemiBold();

    private static void AddCell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor("#D7D5CF").Padding(5).Text(text);

    private static string FormatCurrency(decimal value) =>
        string.Format(IndonesianCulture, "Rp {0:N0}", value);
}
