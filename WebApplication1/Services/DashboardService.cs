using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class DashboardService(ApplicationDbContext context, ReportService reportService, InventoryService inventoryService)
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todayBills = await context.Bills
            .Where(b => b.BillDate >= today && b.BillDate < today.AddDays(1))
            .ToListAsync();

        var monthBills = await context.Bills
            .Where(b => b.BillDate >= monthStart && b.BillDate < monthStart.AddMonths(1))
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

        return new DashboardSummary
        {
            TodaySales = todayBills.Sum(b => b.FinalAmount),
            MonthSales = monthBills.Sum(b => b.FinalAmount),
            TodayBills = todayBills.Count,
            MonthBills = monthBills.Count,
            OutstandingDue = outstandingDue,
            LowStockCount = lowStock.Count,
            ExpiringCount = expiring.Count,
            TotalMedicines = totalMedicines,
            RecentBills = recentBills,
            SalesTrend = salesTrend,
            TopMedicines = topMedicines.Take(5).ToList()
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
}

public class DailySalesPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int BillCount { get; set; }
}
