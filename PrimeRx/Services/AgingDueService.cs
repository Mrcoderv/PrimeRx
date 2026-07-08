using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Helpers;
using PrimeRx.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PrimeRx.Services;

public class AgingDueService(ApplicationDbContext context)
{
    static AgingDueService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<AgingDueReport> GetReportAsync(
        string? partyType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        DateTime? asOnDate = null)
    {
        var effectiveDate = asOnDate ?? DateTime.Today;
        var company = await context.CompanyProfiles.SingleOrDefaultAsync();

        var supplierRows = new List<AgingDueRow>();
        var customerRows = new List<AgingDueRow>();

        if (partyType is null or "Supplier")
        {
            supplierRows = await GetPayableRowsAsync(fromDate, toDate, effectiveDate);
        }

        if (partyType is null or "Customer")
        {
            customerRows = await GetReceivableRowsAsync(fromDate, toDate, effectiveDate);
        }

        return new AgingDueReport
        {
            CompanyName = company?.Name ?? "PrimeRx",
            CompanyAddress = company?.Address ?? string.Empty,
            PartyFilter = partyType,
            FromDate = fromDate,
            ToDate = toDate,
            AsOnDate = asOnDate,
            GeneratedAt = DateTime.Now,
            SupplierRows = [.. supplierRows.OrderBy(r => r.PartyName).ThenBy(r => r.Date)],
            CustomerRows = [.. customerRows.OrderBy(r => r.PartyName).ThenBy(r => r.Date)]
        };
    }

    private async Task<List<AgingDueRow>> GetPayableRowsAsync(
        DateTime? fromDate, DateTime? toDate, DateTime asOnDate)
    {
        var query = context.Payables.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(p => p.DueDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(p => p.DueDate <= toDate.Value.Date);

        return await query
            .Select(p => new AgingDueRow
            {
                PartyName = p.SupplierName,
                PartyType = "Supplier",
                Date = p.DueDate,
                InvoiceNo = p.InvoiceNo,
                Narration = p.Description,
                Amount = p.Amount,
                PaidAmount = p.PaidAmount,
                AgeDays = Math.Max(0, (asOnDate - p.DueDate).Days)
            })
            .ToListAsync();
    }

    private async Task<List<AgingDueRow>> GetReceivableRowsAsync(
        DateTime? fromDate, DateTime? toDate, DateTime asOnDate)
    {
        var query = context.Bills
            .Where(b => b.DueAmount > 0 && b.Status == "Active");

        if (fromDate.HasValue)
            query = query.Where(b => b.BillDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(b => b.BillDate <= toDate.Value.Date);

        return await query
            .Select(b => new AgingDueRow
            {
                PartyName = b.CustomerName,
                PartyType = "Customer",
                Date = b.BillDate,
                InvoiceNo = b.BillNumber,
                Narration = b.PaymentMethod,
                Amount = b.FinalAmount,
                PaidAmount = b.PaidAmount,
                AgeDays = Math.Max(0, (asOnDate - b.BillDate).Days)
            })
            .ToListAsync();
    }

    private static void RenderDueTable(TableDescriptor table, List<AgingDueRow> rows)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(2.5f);
            c.RelativeColumn(1.2f);
            c.RelativeColumn(1.2f);
            c.RelativeColumn(2.5f);
            c.RelativeColumn(1.2f);
            c.RelativeColumn(0.8f);
            c.RelativeColumn(1.2f);
        });

        table.Header(h =>
        {
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Party Name").Bold().FontColor(Colors.White);
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Date").Bold().FontColor(Colors.White);
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Inv. No").Bold().FontColor(Colors.White);
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Narration").Bold().FontColor(Colors.White);
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Amount").Bold().FontColor(Colors.White).AlignRight();
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Age").Bold().FontColor(Colors.White).AlignRight();
            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Balance").Bold().FontColor(Colors.White).AlignRight();
        });

        foreach (var r in rows)
        {
            var bgColor = r.AgeDays > 90
                ? Colors.Red.Lighten5
                : r.AgeDays > 60
                    ? Colors.Orange.Lighten4
                    : r.AgeDays > 30
                        ? Colors.Yellow.Lighten4
                        : Colors.White;

            table.Cell().Background(bgColor).Padding(4).Text(r.PartyName);
            table.Cell().Background(bgColor).Padding(4).Text(r.Date.ToString("dd-MM-yyyy"));
            table.Cell().Background(bgColor).Padding(4).Text(r.InvoiceNo ?? "\u2014");
            table.Cell().Background(bgColor).Padding(4).Text(r.Narration ?? "\u2014");
            table.Cell().Background(bgColor).Padding(4).Text(r.Amount.ToString("N2")).AlignRight();
            table.Cell().Background(bgColor).Padding(4).Text(r.AgeDays.ToString()).AlignRight();
            table.Cell().Background(bgColor).Padding(4).Text(r.Balance.ToString("N2")).AlignRight().Bold();
        }
    }

    public byte[] ExportToPdf(AgingDueReport report)
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

                    var filterParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(report.PartyFilter))
                        filterParts.Add($"Party: {report.PartyFilter}");
                    if (report.FromDate.HasValue)
                        filterParts.Add($"From: {report.FromDate:dd-MM-yyyy}");
                    if (report.ToDate.HasValue)
                        filterParts.Add($"To: {report.ToDate:dd-MM-yyyy}");
                    if (report.AsOnDate.HasValue)
                        filterParts.Add($"As on: {report.AsOnDate:dd-MM-yyyy}");
                    if (filterParts.Count > 0)
                        header.Item().PaddingTop(2)
                            .Text(string.Join(" | ", filterParts))
                            .FontSize(8).FontColor(Colors.Grey.Darken1).AlignCenter();

                    header.Item().PaddingTop(2)
                        .Text($"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1).AlignCenter();
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                });

                page.Content().Column(col =>
                {
                    if (report.SupplierRows.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Supplier Dues (Payables)")
                            .Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(4).Table(table => RenderDueTable(table, report.SupplierRows));
                        col.Item().PaddingTop(4).AlignRight().Text(text =>
                        {
                            text.Span("Supplier Total: ").Bold().FontSize(10);
                            text.Span($"{report.SupplierTotal.ToRs()}").FontSize(10);
                        });
                    }

                    if (report.CustomerRows.Count > 0)
                    {
                        col.Item().PaddingTop(12).Text("Customer Dues (Receivables)")
                            .Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(4).Table(table => RenderDueTable(table, report.CustomerRows));
                        col.Item().PaddingTop(4).AlignRight().Text(text =>
                        {
                            text.Span("Customer Total: ").Bold().FontSize(10);
                            text.Span($"{report.CustomerTotal.ToRs()}").FontSize(10);
                        });
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.Span("Grand Total: ").Bold().FontSize(10);
                        text.Span($"{report.GrandTotal.ToRs()}").Bold().FontSize(11);
                    });
                    footer.Item().PaddingTop(8).AlignCenter().Text("Powered by Prime LogicTech")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }
}
