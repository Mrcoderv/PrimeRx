using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

public class DashboardService(ApplicationDbContext context, ReportService reportService, InventoryService inventoryService, ExpenseService expenseService)
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var todayBills = await context.Bills
            .Where(b => b.BillDate >= today && b.BillDate < today.AddDays(1))
            .ToListAsync();

        var monthBills = await context.Bills
            .Where(b => b.BillDate >= monthStart && b.BillDate < monthEnd)
            .ToListAsync();

        var outstandingDue = await context.Bills.Where(b => b.DueAmount > 0).SumAsync(b => b.DueAmount);
        var lowStock = await inventoryService.GetLowStockAsync();
        var expiring = await inventoryService.GetExpiringSoonAsync();
        var totalMedicines = await context.Medicines.CountAsync(m => m.IsActive);

        var recentBills = await context.Bills
            .OrderByDescending(b => b.BillDate)
            .Take(8)
            .ToListAsync();

        var salesTrend = await GetSalesTrendAsync(7);
        var topMedicines = await reportService.GetMedicineWiseSalesAsync(today.AddDays(-30), today);

        var todayExpenses = await expenseService.GetTotalExpensesAsync(today, today.AddDays(1));
        var monthExpenses = await expenseService.GetTotalExpensesAsync(monthStart, monthEnd);
        var expensesByCategory = await expenseService.GetExpensesByCategoryAsync(today.Year, today.Month);

        var monthSales = monthBills.Sum(b => b.FinalAmount);

        var staffPerformance = monthBills
            .GroupBy(b => string.IsNullOrWhiteSpace(b.StaffName) ? "Unassigned" : b.StaffName!)
            .Select(g => new StaffSalesRow
            {
                StaffName = g.Key,
                BillCount = g.Count(),
                SalesAmount = g.Sum(b => b.FinalAmount)
            })
            .OrderByDescending(r => r.SalesAmount)
            .ToList();

        return new DashboardSummary
        {
            TodaySales = todayBills.Sum(b => b.FinalAmount),
            MonthSales = monthSales,
            TodayBills = todayBills.Count,
            MonthBills = monthBills.Count,
            OutstandingDue = outstandingDue,
            LowStockCount = lowStock.Count,
            ExpiringCount = expiring.Count,
            TotalMedicines = totalMedicines,
            RecentBills = recentBills,
            SalesTrend = salesTrend,
            TopMedicines = topMedicines.Take(5).ToList(),
            TodayExpenses = todayExpenses,
            MonthExpenses = monthExpenses,
            MonthDiscount = monthBills.Sum(b => b.DiscountAmount),
            MonthNet = monthSales - monthExpenses,
            ExpensesByCategory = expensesByCategory
                .Select(kv => new CategoryAmount
                {
                    Category = kv.Key,
                    Label = ExpenseCategories.Display(kv.Key),
                    Amount = kv.Value
                })
                .OrderByDescending(c => c.Amount)
                .ToList(),
            StaffPerformance = staffPerformance
        };
    }

    public async Task<List<DailySalesPoint>> GetSalesTrendAsync(int days)
    {
        var start = DateTime.Today.AddDays(-(days - 1));
        var bills = await context.Bills
            .Where(b => b.BillDate >= start)
            .ToListAsync();

        var points = new List<DailySalesPoint>();
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            var dayBills = bills.Where(b => b.BillDate.Date == date).ToList();
            points.Add(new DailySalesPoint
            {
                Date = date,
                Label = date.ToString("dd MMM"),
                Amount = dayBills.Sum(b => b.FinalAmount),
                BillCount = dayBills.Count
            });
        }

        return points;
    }
}

public class DashboardSummary
{
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public int TodayBills { get; set; }
    public int MonthBills { get; set; }
    public decimal OutstandingDue { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public int TotalMedicines { get; set; }
    public List<Bill> RecentBills { get; set; } = [];
    public List<DailySalesPoint> SalesTrend { get; set; } = [];
    public List<MedicineSalesRow> TopMedicines { get; set; } = [];

    public decimal TodayExpenses { get; set; }
    public decimal MonthExpenses { get; set; }
    public decimal MonthDiscount { get; set; }
    public decimal MonthNet { get; set; }
    public List<CategoryAmount> ExpensesByCategory { get; set; } = [];
    public List<StaffSalesRow> StaffPerformance { get; set; } = [];
}

public class CategoryAmount
{
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class StaffSalesRow
{
    public string StaffName { get; set; } = string.Empty;
    public int BillCount { get; set; }
    public decimal SalesAmount { get; set; }
}

public class DailySalesPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int BillCount { get; set; }
}
