using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PrimeRx.Services;

public class PayableService(ApplicationDbContext context)
{
    static PayableService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<PayableAgingReport> GetPayableAgingReportAsync(
        string? supplier = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = context.Payables.AsQueryable();

        if (!string.IsNullOrWhiteSpace(supplier))
            query = query.Where(p => p.SupplierName.ToLower().Contains(supplier.Trim().ToLower()));

        if (fromDate.HasValue)
            query = query.Where(p => p.DueDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(p => p.DueDate <= toDate.Value.Date);

        var payables = await query
            .OrderBy(p => p.SupplierName)
            .ThenBy(p => p.DueDate)
            .ToListAsync();

        var company = await context.CompanyProfiles.SingleOrDefaultAsync();

        var rows = payables.Select(p => new PayableAgingRow
        {
            Id = p.Id,
            SupplierName = p.SupplierName,
            InvoiceNo = p.InvoiceNo,
            Description = p.Description,
            DueDate = p.DueDate,
            Amount = p.Amount,
            PaidAmount = p.PaidAmount,
            AgeDays = Math.Max(0, (DateTime.Today - p.DueDate).Days),
            Status = p.Status
        }).ToList();

        return new PayableAgingReport
        {
            CompanyName = company?.Name ?? "PrimeRx",
            CompanyAddress = company?.Address ?? string.Empty,
            SupplierFilter = supplier,
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = DateTime.Now,
            Rows = rows
        };
    }

    public byte[] ExportToExcel(PayableAgingReport report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Ageing Dues");

        sheet.Cells[1, 1].Value = report.Title;
        sheet.Cells[1, 1, 1, 6].Merge = true;
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 1].Style.Font.Size = 14;

        sheet.Cells[2, 1].Value = report.CompanyName;
        sheet.Cells[2, 1, 2, 6].Merge = true;

        int headerRow = 4;
        sheet.Cells[headerRow, 1].Value = "Date";
        sheet.Cells[headerRow, 2].Value = "Inv. No";
        sheet.Cells[headerRow, 3].Value = "Narration";
        sheet.Cells[headerRow, 4].Value = "Amount";
        sheet.Cells[headerRow, 5].Value = "Age";
        sheet.Cells[headerRow, 6].Value = "Balance";
        for (int c = 1; c <= 6; c++)
        {
            sheet.Cells[headerRow, c].Style.Font.Bold = true;
            sheet.Cells[headerRow, c].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        }

        var row = headerRow + 1;
        foreach (var r in report.Rows)
        {
            sheet.Cells[row, 1].Value = r.DueDate.ToString("dd-MM-yyyy");
            sheet.Cells[row, 2].Value = r.InvoiceNo ?? "—";
            sheet.Cells[row, 3].Value = $"{r.SupplierName}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" — {r.Description}")}";
            sheet.Cells[row, 4].Value = r.Amount;
            sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[row, 5].Value = r.AgeDays;
            sheet.Cells[row, 6].Value = r.PendingAmount;
            sheet.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
            for (int c = 1; c <= 6; c++)
                sheet.Cells[row, c].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            row++;
        }

        sheet.Cells[row, 3].Value = "Grand Total";
        sheet.Cells[row, 3].Style.Font.Bold = true;
        sheet.Cells[row, 6].Value = report.GrandTotal;
        sheet.Cells[row, 6].Style.Font.Bold = true;
        sheet.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
        for (int c = 1; c <= 6; c++)
            sheet.Cells[row, c].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    public byte[] ExportToPdf(PayableAgingReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text(report.CompanyName).Bold().FontSize(14).AlignCenter();
                    if (!string.IsNullOrWhiteSpace(report.CompanyAddress))
                        header.Item().Text(report.CompanyAddress).AlignCenter().FontSize(9);
                    header.Item().PaddingTop(4).Text(report.Title).Bold().FontSize(12).AlignCenter();
                    header.Item().PaddingTop(2)
                        .Text($"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1).AlignCenter();
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(3f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Date").Bold().FontColor(Colors.White);
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Inv. No").Bold().FontColor(Colors.White);
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Narration").Bold().FontColor(Colors.White);
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Amount").Bold().FontColor(Colors.White).AlignRight();
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Age").Bold().FontColor(Colors.White).AlignRight();
                        h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Balance").Bold().FontColor(Colors.White).AlignRight();
                    });

                    foreach (var r in report.Rows)
                    {
                        var bgColor = r.AgeDays > 90
                            ? Colors.Red.Lighten5
                            : r.AgeDays > 60
                                ? Colors.Orange.Lighten4
                                : r.AgeDays > 30
                                    ? Colors.Yellow.Lighten4
                                    : Colors.White;

                        table.Cell().Background(bgColor).Padding(4).Text(r.DueDate.ToString("dd-MM-yyyy"));
                        table.Cell().Background(bgColor).Padding(4).Text(r.InvoiceNo ?? "—");
                        table.Cell().Background(bgColor).Padding(4).Text(
                            $"{r.SupplierName}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" — {r.Description}")}");
                        table.Cell().Background(bgColor).Padding(4).Text(r.Amount.ToString("N2")).AlignRight();
                        table.Cell().Background(bgColor).Padding(4).Text(r.AgeDays.ToString()).AlignRight();
                        table.Cell().Background(bgColor).Padding(4).Text(r.PendingAmount.ToString("N2")).AlignRight().Bold();
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.Span("Grand Total: ").Bold().FontSize(10);
                        text.Span($"Rs. {report.GrandTotal:N2}").Bold().FontSize(11);
                    });
                    footer.Item().PaddingTop(8).AlignCenter().Text("Powered by Prime LogicTech")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }
}
