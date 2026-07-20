using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class ReportService(ApplicationDbContext context, ExpenseService expenseService)
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
        decimal expenses = await expenseService.GetTotalExpensesAsync(
            from?.Date ?? DateTime.MinValue,
            to?.Date.AddDays(1) ?? DateTime.MaxValue);

        return new ProfitLossReport
        {
            Revenue = revenue,
            Cost = cost,
            Expenses = expenses,
            Profit = revenue - cost - expenses,
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

    public async Task<List<SupplierProfitRow>> GetSupplierProfitReportAsync()
    {
        var result = await context.PurchaseItems
            .Include(pi => pi.Purchase)
            .GroupBy(pi => pi.Purchase.SupplierName)
            .Select(g => new SupplierProfitRow
            {
                SupplierName   = g.Key,
                PurchaseCount  = g.Select(pi => pi.PurchaseId).Distinct().Count(),
                TotalUnits     = g.Sum(pi => pi.Quantity),
                TotalCost      = g.Sum(pi => pi.Quantity * pi.PurchasePrice),
                TotalMrpValue  = g.Sum(pi => pi.Quantity * pi.MRP),
            })
            .ToListAsync();

        return result.OrderByDescending(r => r.PotentialProfit).ToList();
    }

    public async Task<List<MonthlySupplierPurchaseRow>> GetMonthlyPurchaseBySupplierAsync(int year)
    {
        var purchases = await context.Purchases
            .Where(p => p.PurchaseDate.Year == year)
            .OrderBy(p => p.PurchaseDate)
            .ToListAsync();

        return purchases
            .GroupBy(p => new { p.SupplierName, p.PurchaseDate.Month })
            .Select(g => new MonthlySupplierPurchaseRow
            {
                SupplierName  = g.Key.SupplierName,
                Year          = year,
                Month         = g.Key.Month,
                MonthLabel    = new DateTime(year, g.Key.Month, 1).ToString("MMM yyyy"),
                PurchaseCount = g.Count(),
                TotalAmount   = g.Sum(p => p.TotalAmount)
            })
            .OrderBy(r => r.Month).ThenBy(r => r.SupplierName)
            .ToList();
    }

    public async Task<SupplierPayableReport> GetSupplierPayableReportAsync()
    {
        var payables = await context.Payables
            .OrderBy(p => p.SupplierName).ThenByDescending(p => p.DueDate)
            .ToListAsync();

        var grouped = payables
            .GroupBy(p => p.SupplierName)
            .Select(g => new SupplierPayableSummaryRow
            {
                SupplierName  = g.Key,
                PayableCount  = g.Count(),
                TotalAmount   = g.Sum(p => p.Amount),
                PaidAmount    = g.Sum(p => p.PaidAmount),
                PendingAmount = g.Sum(p => p.PendingAmount),
                OverdueCount  = g.Count(p => p.IsOverdue)
            })
            .OrderByDescending(r => r.PendingAmount)
            .ToList();

        return new SupplierPayableReport
        {
            TotalAmount   = payables.Sum(p => p.Amount),
            TotalPaid     = payables.Sum(p => p.PaidAmount),
            TotalPending  = payables.Sum(p => p.PendingAmount),
            OverdueCount  = payables.Count(p => p.IsOverdue),
            Suppliers     = grouped,
            AllPayables   = payables
        };
    }

    public async Task<List<AuditLog>> GetAuditReportAsync(int limit = 200) =>
        await context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();

    public async Task<List<AuditLog>> GetAuditReportByDaysAsync(int days, int limit = 500)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        return await context.AuditLogs
            .Where(a => a.Timestamp >= from)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAuditReportByRangeAsync(DateTime from, DateTime to, int limit = 500)
    {
        var toEnd = to.Date.AddDays(1);
        return await context.AuditLogs
            .Where(a => a.Timestamp >= from && a.Timestamp < toEnd)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
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

    public async Task<PurchaseReportData> GetPurchaseReportAsync(DateTime? from = null, DateTime? to = null, string? supplier = null)
    {
        var query = context.Purchases
            .Include(p => p.Items)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(p => p.PurchaseDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(p => p.PurchaseDate < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(supplier))
            query = query.Where(p => p.SupplierName.ToLower().Contains(supplier.ToLower()));

        var purchases = await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();

        var supplierBreakdown = purchases
            .GroupBy(p => p.SupplierName)
            .Select(g => new SupplierSpendRow
            {
                SupplierName = g.Key,
                PurchaseCount = g.Count(),
                TotalSpend = g.Sum(p => p.TotalAmount)
            })
            .OrderByDescending(s => s.TotalSpend)
            .ToList();

        var topMedicines = purchases
            .SelectMany(p => p.Items)
            .GroupBy(i => i.MedicineName)
            .Select(g => new PurchaseMedicineRow
            {
                MedicineName = g.Key,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalSpend = g.Sum(i => i.Quantity * i.PurchasePrice)
            })
            .OrderByDescending(m => m.TotalSpend)
            .Take(20)
            .ToList();

        var fromLabel = from.HasValue ? from.Value.ToString("dd MMM yyyy") : "All time";
        var toLabel = to.HasValue ? to.Value.ToString("dd MMM yyyy") : "Today";

        return new PurchaseReportData
        {
            Title = $"Purchase Report — {fromLabel} to {toLabel}",
            Purchases = purchases,
            TotalAmount = purchases.Sum(p => p.TotalAmount),
            PurchaseCount = purchases.Count,
            SupplierBreakdown = supplierBreakdown,
            TopMedicines = topMedicines
        };
    }

    public byte[] ExportPurchaseReportToExcel(PurchaseReportData report)
    {
        using var package = new ExcelPackage();

        var sheet = package.Workbook.Worksheets.Add("Purchases");
        sheet.Cells[1, 1].Value = report.Title;
        sheet.Cells[2, 1].Value = "Date";
        sheet.Cells[2, 2].Value = "Invoice #";
        sheet.Cells[2, 3].Value = "Supplier";
        sheet.Cells[2, 4].Value = "Items";
        sheet.Cells[2, 5].Value = "Total (Rs.)";
        sheet.Cells[2, 6].Value = "Recorded By";

        var row = 3;
        foreach (var p in report.Purchases)
        {
            sheet.Cells[row, 1].Value = p.PurchaseDate.ToString("dd-MM-yyyy");
            sheet.Cells[row, 2].Value = p.InvoiceNumber ?? "—";
            sheet.Cells[row, 3].Value = p.SupplierName;
            sheet.Cells[row, 4].Value = p.Items.Count;
            sheet.Cells[row, 5].Value = p.TotalAmount;
            sheet.Cells[row, 6].Value = p.CreatedBy ?? "—";
            row++;
        }
        sheet.Cells[row + 1, 4].Value = "Total:";
        sheet.Cells[row + 1, 5].Value = report.TotalAmount;

        var supplierSheet = package.Workbook.Worksheets.Add("By Supplier");
        supplierSheet.Cells[1, 1].Value = "Supplier";
        supplierSheet.Cells[1, 2].Value = "Purchases";
        supplierSheet.Cells[1, 3].Value = "Total Spend (Rs.)";
        row = 2;
        foreach (var s in report.SupplierBreakdown)
        {
            supplierSheet.Cells[row, 1].Value = s.SupplierName;
            supplierSheet.Cells[row, 2].Value = s.PurchaseCount;
            supplierSheet.Cells[row, 3].Value = s.TotalSpend;
            row++;
        }

        var medSheet = package.Workbook.Worksheets.Add("Top Medicines");
        medSheet.Cells[1, 1].Value = "Medicine";
        medSheet.Cells[1, 2].Value = "Quantity Purchased";
        medSheet.Cells[1, 3].Value = "Total Spend (Rs.)";
        row = 2;
        foreach (var m in report.TopMedicines)
        {
            medSheet.Cells[row, 1].Value = m.MedicineName;
            medSheet.Cells[row, 2].Value = m.TotalQuantity;
            medSheet.Cells[row, 3].Value = m.TotalSpend;
            row++;
        }

        return package.GetAsByteArray();
    }

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

    public byte[] ExportProfitLossToExcel(ProfitLossReport report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Profit & Loss");
        sheet.Cells[1, 1].Value = "Metric";
        sheet.Cells[1, 2].Value = "Amount (Rs.)";
        sheet.Cells[2, 1].Value = "Revenue";
        sheet.Cells[2, 2].Value = report.Revenue;
        sheet.Cells[3, 1].Value = "Cost of Goods";
        sheet.Cells[3, 2].Value = report.Cost;
        sheet.Cells[4, 1].Value = "Expenses";
        sheet.Cells[4, 2].Value = report.Expenses;
        sheet.Cells[5, 1].Value = "Net Profit";
        sheet.Cells[5, 2].Value = report.Profit;
        return package.GetAsByteArray();
    }

    public byte[] ExportDueCollectionToExcel(DueCollectionReport report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Due Collection");
        sheet.Cells[1, 1].Value = "Bill ID";
        sheet.Cells[1, 2].Value = "Payment Date";
        sheet.Cells[1, 3].Value = "Amount Paid";
        sheet.Cells[1, 4].Value = "Method";
        sheet.Cells[1, 5].Value = "Remarks";
        var row = 2;
        foreach (var p in report.Payments)
        {
            sheet.Cells[row, 1].Value = p.BillId;
            sheet.Cells[row, 2].Value = p.PaymentDate.ToString("dd-MM-yyyy");
            sheet.Cells[row, 3].Value = p.AmountPaid;
            sheet.Cells[row, 4].Value = p.PaymentMethod;
            sheet.Cells[row, 5].Value = p.Remarks ?? "—";
            row++;
        }
        sheet.Cells[row + 1, 3].Value = report.TotalCollected;
        return package.GetAsByteArray();
    }

    public byte[] ExportExpiryToExcel(List<Medicine> medicines)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Expiry Alert");
        sheet.Cells[1, 1].Value = "Medicine";
        sheet.Cells[1, 2].Value = "Stock";
        sheet.Cells[1, 3].Value = "Expiry Date";
        sheet.Cells[1, 4].Value = "Manufacturer";
        var row = 2;
        foreach (var m in medicines)
        {
            sheet.Cells[row, 1].Value = m.Name;
            sheet.Cells[row, 2].Value = m.StockQuantity;
            sheet.Cells[row, 3].Value = m.ExpiryDate?.ToString("dd-MM-yyyy");
            sheet.Cells[row, 4].Value = m.Manufacturer ?? "—";
            row++;
        }
        return package.GetAsByteArray();
    }

    public byte[] ExportSupplierProfitToExcel(List<SupplierProfitRow> rows)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Supplier Profit");
        sheet.Cells[1, 1].Value = "Supplier";
        sheet.Cells[1, 2].Value = "Purchases";
        sheet.Cells[1, 3].Value = "Total Units";
        sheet.Cells[1, 4].Value = "Total Cost (Rs.)";
        sheet.Cells[1, 5].Value = "MRP Value (Rs.)";
        sheet.Cells[1, 6].Value = "Potential Profit (Rs.)";
        sheet.Cells[1, 7].Value = "Margin %";
        var row = 2;
        foreach (var r in rows)
        {
            sheet.Cells[row, 1].Value = r.SupplierName;
            sheet.Cells[row, 2].Value = r.PurchaseCount;
            sheet.Cells[row, 3].Value = r.TotalUnits;
            sheet.Cells[row, 4].Value = r.TotalCost;
            sheet.Cells[row, 5].Value = r.TotalMrpValue;
            sheet.Cells[row, 6].Value = r.PotentialProfit;
            sheet.Cells[row, 7].Value = r.MarginPercent;
            row++;
        }
        return package.GetAsByteArray();
    }

    public byte[] ExportSupplierPayablesToExcel(SupplierPayableReport report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Payables");
        sheet.Cells[1, 1].Value = "Supplier";
        sheet.Cells[1, 2].Value = "Entries";
        sheet.Cells[1, 3].Value = "Total (Rs.)";
        sheet.Cells[1, 4].Value = "Paid (Rs.)";
        sheet.Cells[1, 5].Value = "Pending (Rs.)";
        sheet.Cells[1, 6].Value = "Overdue";
        var row = 2;
        foreach (var s in report.Suppliers)
        {
            sheet.Cells[row, 1].Value = s.SupplierName;
            sheet.Cells[row, 2].Value = s.PayableCount;
            sheet.Cells[row, 3].Value = s.TotalAmount;
            sheet.Cells[row, 4].Value = s.PaidAmount;
            sheet.Cells[row, 5].Value = s.PendingAmount;
            sheet.Cells[row, 6].Value = s.OverdueCount;
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
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Bill #").Bold();
                        h.Cell().Text("Customer").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Amount").Bold();
                        h.Cell().Text("Payment").Bold();
                        h.Cell().Text("Status").Bold();
                    });

                    foreach (var bill in report.Bills)
                    {
                        table.Cell().Text(bill.BillNumber);
                        table.Cell().Text(bill.CustomerName);
                        table.Cell().Text(bill.BillDate.ToString("dd-MM-yyyy"));
                        table.Cell().Text(bill.FinalAmount.ToString("N2"));
                        table.Cell().Text(bill.PaymentMethod);
                        table.Cell().Text(bill.PaymentStatus);
                    }
                });
                page.Footer().AlignRight().Text($"Total: {report.TotalSales.ToRs()}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportProfitLossToPdf(ProfitLossReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Profit & Loss Report").Bold().FontSize(16);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                    table.Header(h =>
                    {
                        h.Cell().Text("Metric").Bold();
                        h.Cell().Text("Amount (Rs.)").Bold().AlignRight();
                    });
                    table.Cell().Text("Revenue");
                    table.Cell().Text(report.Revenue.ToString("N2")).AlignRight();
                    table.Cell().Text("Cost of Goods");
                    table.Cell().Text(report.Cost.ToString("N2")).AlignRight();
                    table.Cell().Text("Expenses");
                    table.Cell().Text(report.Expenses.ToString("N2")).AlignRight();
                    table.Cell().Text("Net Profit");
                    table.Cell().Text(report.Profit.ToString("N2")).AlignRight();
                });
            });
        }).GeneratePdf();
    }

    public byte[] ExportDueCollectionToPdf(DueCollectionReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Due Collection Report").Bold().FontSize(16);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Bill ID").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Method").Bold();
                        h.Cell().Text("Amount").Bold().AlignRight();
                    });
                    foreach (var p in report.Payments)
                    {
                        table.Cell().Text(p.BillId.ToString());
                        table.Cell().Text(p.PaymentDate.ToString("dd-MM-yyyy"));
                        table.Cell().Text(p.PaymentMethod);
                        table.Cell().Text(p.AmountPaid.ToString("N2")).AlignRight();
                    }
                });
                page.Footer().AlignRight().Text($"Total Collected: {report.TotalCollected.ToRs()}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportInventoryToPdf(List<Medicine> medicines)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Inventory Report").Bold().FontSize(16);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Medicine").Bold();
                        h.Cell().Text("Stock").Bold().AlignRight();
                        h.Cell().Text("MRP").Bold().AlignRight();
                        h.Cell().Text("Expiry").Bold();
                    });
                    foreach (var m in medicines)
                    {
                        table.Cell().Text(m.Name);
                        table.Cell().Text(m.StockQuantity.ToString()).AlignRight();
                        table.Cell().Text(m.MRP.ToString("N2")).AlignRight();
                        table.Cell().Text(m.ExpiryDate?.ToString("dd-MM-yyyy") ?? "—");
                    }
                });
            });
        }).GeneratePdf();
    }

    public byte[] ExportExpiryToPdf(List<Medicine> medicines)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Expiry Alert Report").Bold().FontSize(16);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Medicine").Bold();
                        h.Cell().Text("Stock").Bold().AlignRight();
                        h.Cell().Text("Expiry Date").Bold();
                    });
                    foreach (var m in medicines)
                    {
                        table.Cell().Text(m.Name);
                        table.Cell().Text(m.StockQuantity.ToString()).AlignRight();
                        table.Cell().Text(m.ExpiryDate?.ToString("dd-MM-yyyy") ?? "—");
                    }
                });
            });
        }).GeneratePdf();
    }

    public byte[] ExportPurchaseReportToPdf(PurchaseReportData report)
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
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Invoice #").Bold();
                        h.Cell().Text("Supplier").Bold();
                        h.Cell().Text("Items").Bold().AlignRight();
                        h.Cell().Text("Amount").Bold().AlignRight();
                    });
                    foreach (var p in report.Purchases)
                    {
                        table.Cell().Text(p.InvoiceNumber ?? "—");
                        table.Cell().Text(p.SupplierName);
                        table.Cell().Text(p.Items.Count.ToString()).AlignRight();
                        table.Cell().Text(p.TotalAmount.ToString("N2")).AlignRight();
                    }
                });
                page.Footer().AlignRight().Text($"Total: {report.TotalAmount.ToRs()}");
            });
        }).GeneratePdf();
    }

    public async Task<VendorReportData> GetVendorReportAsync(string? vendor, DateTime? from, DateTime? to)
    {
        var query = context.Purchases
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(vendor))
            query = query.Where(p => p.SupplierName == vendor);
        if (from.HasValue)
            query = query.Where(p => p.PurchaseDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(p => p.PurchaseDate < to.Value.Date.AddDays(1));

        var purchases = await query.OrderBy(p => p.PurchaseDate).ToListAsync();

        var allVendorNames = await context.Purchases
            .Select(p => p.SupplierName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var payables = await context.Payables
            .Where(pv => string.IsNullOrEmpty(vendor) || pv.SupplierName == vendor)
            .ToListAsync();

        var creditNotes = await context.CreditNotes
            .Where(cn => string.IsNullOrEmpty(vendor) || cn.SupplierName == vendor)
            .ToListAsync();

        var transactions = purchases.Select(p =>
        {
            var supplierPayables = payables.Where(pv => pv.SupplierName == p.SupplierName
                && pv.InvoiceNo == p.InvoiceNumber).ToList();
            var supplierCredits = creditNotes.Where(cn => cn.SupplierName == p.SupplierName
                && cn.PurchaseReturn != null && cn.PurchaseReturn.PurchaseId == p.Id).ToList();

            var paid = supplierPayables.Sum(pv => pv.PaidAmount)
                + supplierCredits.Sum(cn => cn.Amount);
            var due = p.TotalAmount - paid;

            return new VendorTransactionRow
            {
                VendorName = p.SupplierName,
                InvoiceNumber = p.InvoiceNumber ?? $"PO-{p.Id}",
                GRNNumber = $"GRN-{p.Id:D5}",
                Date = p.PurchaseDate,
                PurchaseAmount = p.TotalAmount,
                PaidAmount = Math.Min(paid, p.TotalAmount),
                DueBalance = Math.Max(0, due),
                PaymentStatus = due <= 0 ? "Paid" : paid > 0 ? "Partial" : "Due",
                Medicines = p.Items.Select(i => new VendorMedicineDetail
                {
                    MedicineName = i.MedicineName,
                    BatchNumber = i.BatchNumber,
                    Quantity = i.Quantity,
                    PurchasePrice = i.PurchasePrice,
                    MRP = i.MRP
                }).ToList()
            };
        }).OrderBy(t => t.Date).ToList();

        var vendorGroups = transactions
            .GroupBy(t => t.VendorName)
            .Select(g => new VendorSummaryRow
            {
                VendorName = g.Key,
                TransactionCount = g.Count(),
                TotalPurchase = g.Sum(t => t.PurchaseAmount),
                TotalPaid = g.Sum(t => t.PaidAmount),
                TotalDue = g.Sum(t => t.DueBalance)
            })
            .OrderByDescending(s => s.TotalPurchase)
            .ToList();

        return new VendorReportData
        {
            VendorName = vendor ?? "All Vendors",
            FilterFrom = from,
            FilterTo = to,
            SelectedVendor = vendor,
            Transactions = transactions,
            TotalPurchaseAmount = transactions.Sum(t => t.PurchaseAmount),
            TotalPaidAmount = transactions.Sum(t => t.PaidAmount),
            TotalDueBalance = transactions.Sum(t => t.DueBalance),
            VendorSummaries = vendorGroups,
            AllVendorNames = allVendorNames
        };
    }

    public byte[] ExportVendorReportToPdf(VendorReportData report)
    {
        var title = !string.IsNullOrEmpty(report.SelectedVendor)
            ? $"Vendor Report — {report.SelectedVendor}"
            : "Vendor / Company Report";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Column(col =>
                {
                    col.Item().Text(title).Bold().FontSize(16);
                    if (report.FilterFrom.HasValue || report.FilterTo.HasValue)
                    {
                        var period = $"{report.FilterFrom:dd MMM yyyy} — {(report.FilterTo.HasValue ? report.FilterTo.Value.ToString("dd MMM yyyy") : "Present")}";
                        col.Item().Text(period).FontSize(10).FontColor(Colors.Grey.Medium);
                    }
                });
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Vendor").Bold();
                        h.Cell().Text("Invoice").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Amount").Bold().AlignRight();
                        h.Cell().Text("Paid").Bold().AlignRight();
                        h.Cell().Text("Due").Bold().AlignRight();
                    });
                    foreach (var t in report.Transactions)
                    {
                        table.Cell().Text(t.VendorName);
                        table.Cell().Text(t.InvoiceNumber ?? "—");
                        table.Cell().Text(t.Date.ToString("dd-MM-yyyy"));
                        table.Cell().Text(t.PurchaseAmount.ToString("N2")).AlignRight();
                        table.Cell().Text(t.PaidAmount.ToString("N2")).AlignRight();
                        table.Cell().Text(t.DueBalance.ToString("N2")).AlignRight();
                    }
                });
                page.Footer().AlignRight().Text($"Total Purchase: Rs. {report.TotalPurchaseAmount:N2} | Due: Rs. {report.TotalDueBalance:N2}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportVendorReportToExcel(VendorReportData report)
    {
        using var package = new ExcelPackage();

        var txSheet = package.Workbook.Worksheets.Add("Transactions");
        txSheet.Cells[1, 1].Value = !string.IsNullOrEmpty(report.SelectedVendor)
            ? $"Vendor Report — {report.SelectedVendor}"
            : "Vendor / Company Report — All";

        if (report.FilterFrom.HasValue || report.FilterTo.HasValue)
        {
            txSheet.Cells[2, 1].Value = $"Period: {report.FilterFrom:dd MMM yyyy} — {(report.FilterTo.HasValue ? report.FilterTo.Value.ToString("dd MMM yyyy") : "Present")}";
        }

        var headers = new[] { "Vendor", "Invoice No", "GRN No", "Date", "Purchase (Rs.)", "Paid (Rs.)", "Due (Rs.)", "Status" };
        for (var c = 0; c < headers.Length; c++)
        {
            txSheet.Cells[4, c + 1].Value = headers[c];
            txSheet.Cells[4, c + 1].Style.Font.Bold = true;
        }

        var row = 5;
        foreach (var t in report.Transactions)
        {
            txSheet.Cells[row, 1].Value = t.VendorName;
            txSheet.Cells[row, 2].Value = t.InvoiceNumber ?? "—";
            txSheet.Cells[row, 3].Value = t.GRNNumber ?? "—";
            txSheet.Cells[row, 4].Value = t.Date.ToString("dd-MM-yyyy");
            txSheet.Cells[row, 5].Value = t.PurchaseAmount;
            txSheet.Cells[row, 6].Value = t.PaidAmount;
            txSheet.Cells[row, 7].Value = t.DueBalance;
            txSheet.Cells[row, 8].Value = t.PaymentStatus;
            row++;
        }

        txSheet.Cells[row + 1, 4].Value = "Total:";
        txSheet.Cells[row + 1, 5].Value = report.TotalPurchaseAmount;
        txSheet.Cells[row + 1, 6].Value = report.TotalPaidAmount;
        txSheet.Cells[row, 7].Value = report.TotalDueBalance;
        txSheet.Cells[row + 1, 5].Style.Font.Bold = true;
        txSheet.Cells[row + 1, 6].Style.Font.Bold = true;
        txSheet.Cells[row + 1, 7].Style.Font.Bold = true;

        txSheet.Cells.AutoFitColumns();

        var sumSheet = package.Workbook.Worksheets.Add("Summary");
        sumSheet.Cells[1, 1].Value = "Vendor";
        sumSheet.Cells[1, 2].Value = "Transactions";
        sumSheet.Cells[1, 3].Value = "Total Purchase (Rs.)";
        sumSheet.Cells[1, 4].Value = "Total Paid (Rs.)";
        sumSheet.Cells[1, 5].Value = "Total Due (Rs.)";
        sumSheet.Cells[1, 1].Style.Font.Bold = true;
        sumSheet.Cells[1, 2].Style.Font.Bold = true;
        sumSheet.Cells[1, 3].Style.Font.Bold = true;
        sumSheet.Cells[1, 4].Style.Font.Bold = true;
        sumSheet.Cells[1, 5].Style.Font.Bold = true;

        row = 2;
        foreach (var s in report.VendorSummaries)
        {
            sumSheet.Cells[row, 1].Value = s.VendorName;
            sumSheet.Cells[row, 2].Value = s.TransactionCount;
            sumSheet.Cells[row, 3].Value = s.TotalPurchase;
            sumSheet.Cells[row, 4].Value = s.TotalPaid;
            sumSheet.Cells[row, 5].Value = s.TotalDue;
            row++;
        }
        sumSheet.Cells[row + 1, 2].Value = "Total:";
        sumSheet.Cells[row + 1, 3].Value = report.TotalPurchaseAmount;
        sumSheet.Cells[row + 1, 4].Value = report.TotalPaidAmount;
        sumSheet.Cells[row + 1, 5].Value = report.TotalDueBalance;
        sumSheet.Cells.AutoFitColumns();

        if (report.Transactions.Any() && report.Transactions.First().Medicines.Any())
        {
            var medSheet = package.Workbook.Worksheets.Add("Medicines");
            medSheet.Cells[1, 1].Value = "Invoice";
            medSheet.Cells[1, 2].Value = "Medicine";
            medSheet.Cells[1, 3].Value = "Batch";
            medSheet.Cells[1, 4].Value = "Qty";
            medSheet.Cells[1, 5].Value = "Purchase Price";
            medSheet.Cells[1, 6].Value = "MRP";
            medSheet.Cells[1, 1].Style.Font.Bold = true;
            medSheet.Cells[1, 2].Style.Font.Bold = true;
            medSheet.Cells[1, 3].Style.Font.Bold = true;
            medSheet.Cells[1, 4].Style.Font.Bold = true;
            medSheet.Cells[1, 5].Style.Font.Bold = true;
            medSheet.Cells[1, 6].Style.Font.Bold = true;

            row = 2;
            foreach (var t in report.Transactions)
            {
                foreach (var m in t.Medicines)
                {
                    medSheet.Cells[row, 1].Value = t.InvoiceNumber ?? "—";
                    medSheet.Cells[row, 2].Value = m.MedicineName;
                    medSheet.Cells[row, 3].Value = m.BatchNumber ?? "—";
                    medSheet.Cells[row, 4].Value = m.Quantity;
                    medSheet.Cells[row, 5].Value = m.PurchasePrice;
                    medSheet.Cells[row, 6].Value = m.MRP;
                    row++;
                }
            }
            medSheet.Cells.AutoFitColumns();
        }

        return package.GetAsByteArray();
    }

    public byte[] ExportAuditFinancialToPdf(AuditFinancialData report)
    {
        return Document.Create(container =>
        {
            foreach (var section in BuildAuditPdfSections(report))
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header().Column(col =>
                    {
                        col.Item().Text(report.CompanyName).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text(report.CompanyAddress).FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().Text($"PAN: {report.CompanyPAN} | Phone: {report.CompanyPhone}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text(section.Title).Bold().FontSize(13).Underline();
                        col.Item().PaddingTop(8).Table(section.Table);
                    });
                    page.Footer().AlignCenter().Text($"Audit Report — {report.CurrentYear.Label} | Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }
        }).GeneratePdf();
    }

    public byte[] ExportAuditFinancialToExcel(AuditFinancialData report)
    {
        using var package = new ExcelPackage();
        BuildAuditExcelSheets(package, report);
        return package.GetAsByteArray();
    }

    public byte[] ExportBalanceSheetToExcel(AuditFinancialData report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Balance Sheet");
        BuildBalanceSheetExcel(sheet, report);
        return package.GetAsByteArray();
    }

    public byte[] ExportIncomeStatementToExcel(AuditFinancialData report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Income Statement");
        BuildIncomeStatementExcel(sheet, report);
        return package.GetAsByteArray();
    }

    public byte[] ExportCashFlowToExcel(AuditFinancialData report)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Cash Flow");
        BuildCashFlowExcel(sheet, report);
        return package.GetAsByteArray();
    }

    private static void BuildAuditExcelSheets(ExcelPackage package, AuditFinancialData report)
    {
        var bsSheet = package.Workbook.Worksheets.Add("Balance Sheet");
        BuildBalanceSheetExcel(bsSheet, report);

        var isSheet = package.Workbook.Worksheets.Add("Income Statement");
        BuildIncomeStatementExcel(isSheet, report);

        var cfSheet = package.Workbook.Worksheets.Add("Cash Flow");
        BuildCashFlowExcel(cfSheet, report);

        var notesSheet = package.Workbook.Worksheets.Add("Notes");
        notesSheet.Cells[1, 1].Value = "Notes to the Accounts";
        notesSheet.Cells[1, 1].Style.Font.Bold = true;
        notesSheet.Cells[1, 1].Style.Font.Size = 14;
        var nr = 3;
        foreach (var note in report.Notes)
        {
            notesSheet.Cells[nr, 1].Value = $"Note {note.NoteNumber}: {note.Title}";
            notesSheet.Cells[nr, 1].Style.Font.Bold = true;
            nr++;
            notesSheet.Cells[nr, 1].Value = note.Content;
            notesSheet.Cells[nr, 1].Style.WrapText = true;
            nr += 2;
        }
        notesSheet.Cells.AutoFitColumns();
    }

    private static void BuildBalanceSheetExcel(ExcelWorksheet sheet, AuditFinancialData report)
    {
        var cur = report.CurrentBalanceSheet;
        var prev = report.PreviousBalanceSheet;
        sheet.Cells[1, 1].Value = $"Statement of Financial Position (Balance Sheet)";
        sheet.Cells[2, 1].Value = $"Fiscal Year: {report.CurrentYear.Label}";
        sheet.Cells[3, 1].Value = "Particulars";
        sheet.Cells[3, 2].Value = $"Current Year ({report.CurrentYear.Label})";
        sheet.Cells[3, 3].Value = $"Previous Year ({report.CurrentYear.Previous.Label})";
        sheet.Cells[3, 1].Style.Font.Bold = true;
        sheet.Cells[3, 2].Style.Font.Bold = true;
        sheet.Cells[3, 3].Style.Font.Bold = true;

        var r = 5;
        void AddRow(string label, decimal current, decimal previous, bool bold = false)
        {
            sheet.Cells[r, 1].Value = label;
            sheet.Cells[r, 2].Value = current;
            sheet.Cells[r, 3].Value = previous;
            sheet.Cells[r, 2].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[r, 3].Style.Numberformat.Format = "#,##0.00";
            if (bold) { sheet.Cells[r, 1].Style.Font.Bold = true; sheet.Cells[r, 2].Style.Font.Bold = true; sheet.Cells[r, 3].Style.Font.Bold = true; }
            r++;
        }

        AddRow("ASSETS", 0, 0, true);
        AddRow("  Current Assets", 0, 0, true);
        AddRow("    Cash and Bank", cur.CashAndBank, prev.CashAndBank);
        AddRow("    Accounts Receivable", cur.AccountsReceivable, prev.AccountsReceivable);
        AddRow("    Inventory", cur.Inventory, prev.Inventory);
        AddRow("    Other Current Assets", cur.OtherCurrentAssets, prev.OtherCurrentAssets);
        AddRow("  Total Current Assets", cur.TotalCurrentAssets, prev.TotalCurrentAssets, true);
        AddRow("  Fixed Assets (Net)", cur.TotalFixedAssets, prev.TotalFixedAssets);
        AddRow("TOTAL ASSETS", cur.TotalAssets, prev.TotalAssets, true);
        r++;
        AddRow("LIABILITIES & EQUITY", 0, 0, true);
        AddRow("  Current Liabilities", 0, 0, true);
        AddRow("    Accounts Payable", cur.AccountsPayable, prev.AccountsPayable);
        AddRow("    Due to Suppliers", cur.DueToSuppliers, prev.DueToSuppliers);
        AddRow("  Total Current Liabilities", cur.TotalCurrentLiabilities, prev.TotalCurrentLiabilities, true);
        AddRow("  Total Long-term Liabilities", cur.TotalLongTermLiabilities, prev.TotalLongTermLiabilities);
        AddRow("TOTAL LIABILITIES", cur.TotalLiabilities, prev.TotalLiabilities, true);
        r++;
        AddRow("  Owner's Equity", cur.OwnerEquity, prev.OwnerEquity);
        AddRow("  Retained Earnings", cur.RetainedEarnings, prev.RetainedEarnings);
        AddRow("  Net Profit / (Loss)", cur.NetProfitLoss, prev.NetProfitLoss);
        AddRow("TOTAL EQUITY", cur.TotalEquity, prev.TotalEquity, true);
        AddRow("TOTAL LIABILITIES & EQUITY", cur.TotalLiabilitiesAndEquity, prev.TotalLiabilitiesAndEquity, true);
        sheet.Cells.AutoFitColumns();
    }

    private static void BuildIncomeStatementExcel(ExcelWorksheet sheet, AuditFinancialData report)
    {
        var cur = report.CurrentIncome;
        var prev = report.PreviousIncome;
        sheet.Cells[1, 1].Value = $"Statement of Income (Profit & Loss)";
        sheet.Cells[2, 1].Value = $"Fiscal Year: {report.CurrentYear.Label}";
        sheet.Cells[3, 1].Value = "Particulars";
        sheet.Cells[3, 2].Value = $"Current Year ({report.CurrentYear.Label})";
        sheet.Cells[3, 3].Value = $"Previous Year ({report.CurrentYear.Previous.Label})";
        sheet.Cells[3, 1].Style.Font.Bold = true;
        sheet.Cells[3, 2].Style.Font.Bold = true;
        sheet.Cells[3, 3].Style.Font.Bold = true;

        var r = 5;
        void AddRow(string label, decimal current, decimal previous, bool bold = false)
        {
            sheet.Cells[r, 1].Value = label;
            sheet.Cells[r, 2].Value = current;
            sheet.Cells[r, 3].Value = previous;
            sheet.Cells[r, 2].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[r, 3].Style.Numberformat.Format = "#,##0.00";
            if (bold) { sheet.Cells[r, 1].Style.Font.Bold = true; sheet.Cells[r, 2].Style.Font.Bold = true; sheet.Cells[r, 3].Style.Font.Bold = true; }
            r++;
        }

        AddRow("REVENUE", 0, 0, true);
        AddRow("  Sales Revenue", cur.SalesRevenue, prev.SalesRevenue);
        AddRow("  Less: Returns & Discounts", cur.SalesReturns, prev.SalesReturns);
        AddRow("  Net Sales", cur.NetSales, prev.NetSales, true);
        r++;
        AddRow("COST OF GOODS SOLD", cur.CostOfGoodsSold, prev.CostOfGoodsSold, true);
        AddRow("GROSS PROFIT", cur.GrossProfit, prev.GrossProfit, true);
        r++;
        AddRow("OPERATING EXPENSES", 0, 0, true);
        AddRow("  Salary Expense", cur.SalaryExpense, prev.SalaryExpense);
        AddRow("  Rent Expense", cur.RentExpense, prev.RentExpense);
        AddRow("  Utilities Expense", cur.UtilitiesExpense, prev.UtilitiesExpense);
        AddRow("  Other Expenses", cur.OtherExpenses, prev.OtherExpenses);
        AddRow("  Total Operating Expenses", cur.TotalOperatingExpenses, prev.TotalOperatingExpenses, true);
        r++;
        AddRow("OPERATING PROFIT", cur.OperatingProfit, prev.OperatingProfit, true);
        AddRow("NET PROFIT", cur.NetProfit, prev.NetProfit, true);
        r++;
        AddRow($"Bills: {cur.BillCount} | Purchases: {cur.PurchaseCount}", 0, 0);
        sheet.Cells.AutoFitColumns();
    }

    private static void BuildCashFlowExcel(ExcelWorksheet sheet, AuditFinancialData report)
    {
        var cur = report.CurrentCashFlow;
        var prev = report.PreviousCashFlow;
        sheet.Cells[1, 1].Value = $"Statement of Cash Flow";
        sheet.Cells[2, 1].Value = $"Fiscal Year: {report.CurrentYear.Label}";
        sheet.Cells[3, 1].Value = "Particulars";
        sheet.Cells[3, 2].Value = $"Current Year ({report.CurrentYear.Label})";
        sheet.Cells[3, 3].Value = $"Previous Year ({report.CurrentYear.Previous.Label})";
        sheet.Cells[3, 1].Style.Font.Bold = true;
        sheet.Cells[3, 2].Style.Font.Bold = true;
        sheet.Cells[3, 3].Style.Font.Bold = true;

        var r = 5;
        void AddRow(string label, decimal current, decimal previous, bool bold = false)
        {
            sheet.Cells[r, 1].Value = label;
            sheet.Cells[r, 2].Value = current;
            sheet.Cells[r, 3].Value = previous;
            sheet.Cells[r, 2].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[r, 3].Style.Numberformat.Format = "#,##0.00";
            if (bold) { sheet.Cells[r, 1].Style.Font.Bold = true; sheet.Cells[r, 2].Style.Font.Bold = true; sheet.Cells[r, 3].Style.Font.Bold = true; }
            r++;
        }

        AddRow("CASH FLOWS FROM OPERATING ACTIVITIES", 0, 0, true);
        AddRow("  Cash received from sales", cur.CashFromSales, prev.CashFromSales);
        AddRow("  Cash received from receivables", cur.CashFromReceivables, prev.CashFromReceivables);
        AddRow("  Total Operating Inflow", cur.TotalOperatingInflow, prev.TotalOperatingInflow, true);
        r++;
        AddRow("  Cash paid to suppliers", cur.CashPaidToSuppliers, prev.CashPaidToSuppliers);
        AddRow("  Cash paid for expenses", cur.CashPaidForExpenses, prev.CashPaidForExpenses);
        AddRow("  Cash paid for salaries", cur.CashPaidForSalaries, prev.CashPaidForSalaries);
        AddRow("  Total Operating Outflow", cur.TotalOperatingOutflow, prev.TotalOperatingOutflow, true);
        r++;
        AddRow("NET OPERATING CASH FLOW", cur.NetOperatingCashFlow, prev.NetOperatingCashFlow, true);
        r++;
        AddRow("Opening Cash Balance", cur.OpeningCashBalance, prev.OpeningCashBalance);
        AddRow("CLOSING CASH BALANCE", cur.ClosingCashBalance, prev.ClosingCashBalance, true);
        sheet.Cells.AutoFitColumns();
    }

    private List<(string Title, Action<TableDescriptor> Table)> BuildAuditPdfSections(AuditFinancialData report)
    {
        var sections = new List<(string, Action<TableDescriptor>)>();
        sections.Add(("Statement of Financial Position (Balance Sheet)", table => BuildBalanceSheetPdfTable(table, report)));
        sections.Add(("Statement of Income (Profit & Loss Account)", table => BuildIncomeStatementPdfTable(table, report)));
        sections.Add(("Statement of Cash Flow", table => BuildCashFlowPdfTable(table, report)));
        sections.Add(("Notes to the Accounts & Significant Accounting Policies", table => BuildNotesPdfTable(table, report)));
        return sections;
    }

    private static void BuildBalanceSheetPdfTable(TableDescriptor table, AuditFinancialData report)
    {
        var cur = report.CurrentBalanceSheet;
        var prev = report.PreviousBalanceSheet;
        table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(2); c.RelativeColumn(2); });
        table.Header(h =>
        {
            h.Cell().Text("Particulars").Bold();
            h.Cell().Text($"FY {report.CurrentYear.Label}").Bold().AlignRight();
            h.Cell().Text($"FY {report.CurrentYear.Previous.Label}").Bold().AlignRight();
        });
        void AddRow(string label, decimal curVal, decimal prevVal, bool bold = false)
        {
            var cellLabel = bold ? table.Cell().Text(label).Bold() : table.Cell().Text(label);
            var cellCur = bold ? table.Cell().Text(curVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(curVal.ToString("N2")).AlignRight();
            var cellPrev = bold ? table.Cell().Text(prevVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(prevVal.ToString("N2")).AlignRight();
        }
        AddRow("ASSETS", 0, 0, true);
        AddRow("  Cash and Bank", cur.CashAndBank, prev.CashAndBank);
        AddRow("  Accounts Receivable", cur.AccountsReceivable, prev.AccountsReceivable);
        AddRow("  Inventory", cur.Inventory, prev.Inventory);
        AddRow("  Total Current Assets", cur.TotalCurrentAssets, prev.TotalCurrentAssets, true);
        AddRow("  Fixed Assets (Net)", cur.TotalFixedAssets, prev.TotalFixedAssets);
        AddRow("TOTAL ASSETS", cur.TotalAssets, prev.TotalAssets, true);
        AddRow("LIABILITIES & EQUITY", 0, 0, true);
        AddRow("  Accounts Payable", cur.AccountsPayable, prev.AccountsPayable);
        AddRow("  Due to Suppliers", cur.DueToSuppliers, prev.DueToSuppliers);
        AddRow("  Total Current Liabilities", cur.TotalCurrentLiabilities, prev.TotalCurrentLiabilities, true);
        AddRow("TOTAL LIABILITIES", cur.TotalLiabilities, prev.TotalLiabilities, true);
        AddRow("  Owner's Equity", cur.OwnerEquity, prev.OwnerEquity);
        AddRow("  Net Profit / (Loss)", cur.NetProfitLoss, prev.NetProfitLoss);
        AddRow("TOTAL EQUITY", cur.TotalEquity, prev.TotalEquity, true);
        AddRow("TOTAL LIABILITIES & EQUITY", cur.TotalLiabilitiesAndEquity, prev.TotalLiabilitiesAndEquity, true);
    }

    private static void BuildIncomeStatementPdfTable(TableDescriptor table, AuditFinancialData report)
    {
        var cur = report.CurrentIncome;
        var prev = report.PreviousIncome;
        table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(2); c.RelativeColumn(2); });
        table.Header(h =>
        {
            h.Cell().Text("Particulars").Bold();
            h.Cell().Text($"FY {report.CurrentYear.Label}").Bold().AlignRight();
            h.Cell().Text($"FY {report.CurrentYear.Previous.Label}").Bold().AlignRight();
        });
        void AddRow(string label, decimal curVal, decimal prevVal, bool bold = false)
        {
            var cellLabel = bold ? table.Cell().Text(label).Bold() : table.Cell().Text(label);
            var cellCur = bold ? table.Cell().Text(curVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(curVal.ToString("N2")).AlignRight();
            var cellPrev = bold ? table.Cell().Text(prevVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(prevVal.ToString("N2")).AlignRight();
        }
        AddRow("Sales Revenue", cur.SalesRevenue, prev.SalesRevenue);
        AddRow("Less: Returns & Discounts", cur.SalesReturns, prev.SalesReturns);
        AddRow("Net Sales", cur.NetSales, prev.NetSales, true);
        AddRow("Cost of Goods Sold", cur.CostOfGoodsSold, prev.CostOfGoodsSold);
        AddRow("GROSS PROFIT", cur.GrossProfit, prev.GrossProfit, true);
        AddRow("Salary Expense", cur.SalaryExpense, prev.SalaryExpense);
        AddRow("Rent Expense", cur.RentExpense, prev.RentExpense);
        AddRow("Utilities Expense", cur.UtilitiesExpense, prev.UtilitiesExpense);
        AddRow("Other Expenses", cur.OtherExpenses, prev.OtherExpenses);
        AddRow("Total Operating Expenses", cur.TotalOperatingExpenses, prev.TotalOperatingExpenses, true);
        AddRow("NET PROFIT", cur.NetProfit, prev.NetProfit, true);
    }

    private static void BuildCashFlowPdfTable(TableDescriptor table, AuditFinancialData report)
    {
        var cur = report.CurrentCashFlow;
        var prev = report.PreviousCashFlow;
        table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(2); c.RelativeColumn(2); });
        table.Header(h =>
        {
            h.Cell().Text("Particulars").Bold();
            h.Cell().Text($"FY {report.CurrentYear.Label}").Bold().AlignRight();
            h.Cell().Text($"FY {report.CurrentYear.Previous.Label}").Bold().AlignRight();
        });
        void AddRow(string label, decimal curVal, decimal prevVal, bool bold = false)
        {
            var cellLabel = bold ? table.Cell().Text(label).Bold() : table.Cell().Text(label);
            var cellCur = bold ? table.Cell().Text(curVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(curVal.ToString("N2")).AlignRight();
            var cellPrev = bold ? table.Cell().Text(prevVal.ToString("N2")).AlignRight().Bold() : table.Cell().Text(prevVal.ToString("N2")).AlignRight();
        }
        AddRow("Cash from Sales", cur.CashFromSales, prev.CashFromSales);
        AddRow("Cash from Receivables", cur.CashFromReceivables, prev.CashFromReceivables);
        AddRow("Total Operating Inflow", cur.TotalOperatingInflow, prev.TotalOperatingInflow, true);
        AddRow("Cash paid to Suppliers", cur.CashPaidToSuppliers, prev.CashPaidToSuppliers);
        AddRow("Cash paid for Expenses", cur.CashPaidForExpenses, prev.CashPaidForExpenses);
        AddRow("Cash paid for Salaries", cur.CashPaidForSalaries, prev.CashPaidForSalaries);
        AddRow("Total Operating Outflow", cur.TotalOperatingOutflow, prev.TotalOperatingOutflow, true);
        AddRow("Net Operating Cash Flow", cur.NetOperatingCashFlow, prev.NetOperatingCashFlow, true);
        AddRow("Opening Cash Balance", cur.OpeningCashBalance, prev.OpeningCashBalance);
        AddRow("Closing Cash Balance", cur.ClosingCashBalance, prev.ClosingCashBalance, true);
    }

    private static void BuildNotesPdfTable(TableDescriptor table, AuditFinancialData report)
    {
        table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(6); });
        table.Header(h =>
        {
            h.Cell().Text("Note").Bold();
            h.Cell().Text("Description").Bold();
        });
        foreach (var note in report.Notes)
        {
            table.Cell().Text(note.NoteNumber.ToString()).Bold();
            table.Cell().ColumnSpan(1).Text($"{note.Title}\n{note.Content}");
        }
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
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public int BillCount { get; set; }
}

public class PurchaseReportData
{
    public string Title { get; set; } = string.Empty;
    public List<Purchase> Purchases { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public int PurchaseCount { get; set; }
    public List<SupplierSpendRow> SupplierBreakdown { get; set; } = [];
    public List<PurchaseMedicineRow> TopMedicines { get; set; } = [];
}

public class SupplierSpendRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public decimal TotalSpend { get; set; }
}

public class PurchaseMedicineRow
{
    public string MedicineName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalSpend { get; set; }
}

public class DueCollectionReport
{
    public decimal TotalCollected { get; set; }
    public decimal OutstandingDue { get; set; }
    public List<DuePayment> Payments { get; set; } = [];
}

public class SupplierProfitRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalMrpValue { get; set; }
    public decimal PotentialProfit => TotalMrpValue - TotalCost;
    public decimal MarginPercent => TotalCost > 0 ? Math.Round(PotentialProfit / TotalCost * 100, 1) : 0;
}

public class MonthlySupplierPurchaseRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SupplierPayableReport
{
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int OverdueCount { get; set; }
    public List<SupplierPayableSummaryRow> Suppliers { get; set; } = [];
    public List<Payable> AllPayables { get; set; } = [];
}

public class SupplierPayableSummaryRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int PayableCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int OverdueCount { get; set; }
}
