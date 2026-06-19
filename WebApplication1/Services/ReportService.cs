using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class ReportService(ApplicationDbContext context)
{
    static ReportService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    public async Task<SalesReportData> GetDailySalesAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await BuildSalesReportAsync(
            await GetBillsInRangeAsync(start, end),
            $"Daily Sales - {date:dd MMM yyyy}");
    }

    public async Task<SalesReportData> GetMonthlySalesAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        return await BuildSalesReportAsync(
            await GetBillsInRangeAsync(start, end),
            $"Monthly Sales - {start:MMMM yyyy}");
    }

    public async Task<List<MedicineSalesRow>> GetMedicineWiseSalesAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = context.SaleItems
            .Include(s => s.Bill)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.Bill.BillDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(s => s.Bill.BillDate < to.Value.Date.AddDays(1));

        return await query
            .GroupBy(s => new { s.MedicineId, s.MedicineName })
            .Select(g => new MedicineSalesRow
            {
                MedicineName = g.Key.MedicineName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToListAsync();
    }

    public async Task<ProfitLossReport> GetProfitLossAsync(DateTime? from = null, DateTime? to = null)
    {
        var bills = await GetBillsInRangeAsync(
            from?.Date ?? DateTime.MinValue,
            to?.Date.AddDays(1) ?? DateTime.MaxValue);

        var billIds = bills.Select(b => b.Id).ToList();

        var saleItems = await context.SaleItems
            .Where(s => billIds.Contains(s.BillId))
            .ToListAsync();

        var medicineIds = saleItems.Select(s => s.MedicineId).Distinct().ToList();
        var medicines = await context.Medicines
            .Where(m => medicineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.PurchasePrice);

        decimal revenue = bills.Sum(b => b.FinalAmount);
        decimal cost = saleItems.Sum(s => medicines.GetValueOrDefault(s.MedicineId) * s.Quantity);

        return new ProfitLossReport
        {
            Revenue = revenue,
            Cost = cost,
            Profit = revenue - cost,
            BillCount = bills.Count
        };
    }

    public async Task<DueCollectionReport> GetDueCollectionAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = context.DuePayments.AsQueryable();

        if (from.HasValue)
            query = query.Where(d => d.PaymentDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(d => d.PaymentDate < to.Value.Date.AddDays(1));

        var payments = await query.OrderByDescending(d => d.PaymentDate).ToListAsync();
        var outstanding = await context.Bills.Where(b => b.DueAmount > 0).SumAsync(b => b.DueAmount);

        return new DueCollectionReport
        {
            TotalCollected = payments.Sum(p => p.AmountPaid),
            OutstandingDue = outstanding,
            Payments = payments
        };
    }

    public async Task<List<Medicine>> GetInventoryReportAsync() =>
        await context.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

    public async Task<List<Medicine>> GetExpiryReportAsync(int days = 90) =>
        await context.Medicines
            .Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate <= DateTime.Now.AddDays(days))
            .OrderBy(m => m.ExpiryDate)
            .ToListAsync();

    public byte[] ExportSalesToExcel(SalesReportData report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Sales Report");

        sheet.Cells[1, 1].Value = report.Title;
        sheet.Cells[2, 1].Value = "Bill Number";
        sheet.Cells[2, 2].Value = "Date";
        sheet.Cells[2, 3].Value = "Customer";
        sheet.Cells[2, 4].Value = "Phone";
        sheet.Cells[2, 5].Value = "Amount";
        sheet.Cells[2, 6].Value = "Payment";
        sheet.Cells[2, 7].Value = "Status";

        var row = 3;
        foreach (var bill in report.Bills)
        {
            sheet.Cells[row, 1].Value = bill.BillNumber;
            sheet.Cells[row, 2].Value = bill.BillDate.ToString("dd-MM-yyyy HH:mm");
            sheet.Cells[row, 3].Value = bill.CustomerName;
            sheet.Cells[row, 4].Value = bill.CustomerPhone;
            sheet.Cells[row, 5].Value = bill.FinalAmount;
            sheet.Cells[row, 6].Value = bill.PaymentMethod;
            sheet.Cells[row, 7].Value = bill.PaymentStatus;
            row++;
        }

        sheet.Cells[row + 1, 4].Value = "Total:";
        sheet.Cells[row + 1, 5].Value = report.TotalSales;

        return package.GetAsByteArray();
    }

    public byte[] ExportMedicineSalesToExcel(List<MedicineSalesRow> rows)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Medicine Sales");

        sheet.Cells[1, 1].Value = "Medicine";
        sheet.Cells[1, 2].Value = "Quantity";
        sheet.Cells[1, 3].Value = "Amount";

        var row = 2;
        foreach (var item in rows)
        {
            sheet.Cells[row, 1].Value = item.MedicineName;
            sheet.Cells[row, 2].Value = item.TotalQuantity;
            sheet.Cells[row, 3].Value = item.TotalAmount;
            row++;
        }

        return package.GetAsByteArray();
    }

    public byte[] ExportInventoryToExcel(List<Medicine> medicines)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Inventory");

        sheet.Cells[1, 1].Value = "Medicine";
        sheet.Cells[1, 2].Value = "Generic";
        sheet.Cells[1, 3].Value = "Stock";
        sheet.Cells[1, 4].Value = "MRP";
        sheet.Cells[1, 5].Value = "Purchase Price";
        sheet.Cells[1, 6].Value = "Expiry";

        var row = 2;
        foreach (var m in medicines)
        {
            sheet.Cells[row, 1].Value = m.Name;
            sheet.Cells[row, 2].Value = m.GenericName;
            sheet.Cells[row, 3].Value = m.StockQuantity;
            sheet.Cells[row, 4].Value = m.MRP;
            sheet.Cells[row, 5].Value = m.PurchasePrice;
            sheet.Cells[row, 6].Value = m.ExpiryDate?.ToString("dd-MM-yyyy");
            row++;
        }

        return package.GetAsByteArray();
    }

    public byte[] ExportSalesToPdf(SalesReportData report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text(report.Title).Bold().FontSize(16);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Bill #").Bold();
                        h.Cell().Text("Customer").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Amount").Bold();
                    });

                    foreach (var bill in report.Bills)
                    {
                        table.Cell().Text(bill.BillNumber);
                        table.Cell().Text(bill.CustomerName);
                        table.Cell().Text(bill.BillDate.ToString("dd-MM-yyyy"));
                        table.Cell().Text(bill.FinalAmount.ToString("N2"));
                    }
                });
                page.Footer().AlignRight().Text($"Total: {report.TotalSales:N2}");
            });
        }).GeneratePdf();
    }

    private async Task<List<Bill>> GetBillsInRangeAsync(DateTime start, DateTime end)
    {
        if (end == DateTime.MaxValue)
        {
            return await context.Bills
                .Where(b => b.BillDate >= start)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        return await context.Bills
            .Where(b => b.BillDate >= start && b.BillDate < end)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();
    }

    private static Task<SalesReportData> BuildSalesReportAsync(List<Bill> bills, string title)
    {
        return Task.FromResult(new SalesReportData
        {
            Title = title,
            Bills = bills,
            TotalSales = bills.Sum(b => b.FinalAmount),
            BillCount = bills.Count
        });
    }
}

public class SalesReportData
{
    public string Title { get; set; } = string.Empty;
    public List<Bill> Bills { get; set; } = [];
    public decimal TotalSales { get; set; }
    public int BillCount { get; set; }
}

public class MedicineSalesRow
{
    public string MedicineName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ProfitLossReport
{
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int BillCount { get; set; }
}

public class DueCollectionReport
{
    public decimal TotalCollected { get; set; }
    public decimal OutstandingDue { get; set; }
    public List<DuePayment> Payments { get; set; } = [];
}
