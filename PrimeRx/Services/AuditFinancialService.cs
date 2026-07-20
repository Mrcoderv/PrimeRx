using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class AuditFinancialService(ApplicationDbContext context)
{
    public async Task<AuditFinancialData> GetAuditFinancialDataAsync(FiscalYear fiscalYear)
    {
        var company = await context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync();

        var data = new AuditFinancialData
        {
            CurrentYear = fiscalYear,
            CompanyName = company?.Name ?? "Pharmacy Name",
            CompanyAddress = company?.Address ?? "Address",
            CompanyPhone = company?.Phone ?? "",
            CompanyPAN = company?.PAN ?? "",
            CompanyLogoPath = company?.LogoPath
        };

        data.CurrentBalanceSheet = await BuildBalanceSheetAsync(fiscalYear.StartDate, fiscalYear.EndDate);
        data.PreviousBalanceSheet = await BuildBalanceSheetAsync(fiscalYear.Previous.StartDate, fiscalYear.Previous.EndDate);

        data.CurrentIncome = await BuildIncomeStatementAsync(fiscalYear.StartDate, fiscalYear.EndDate);
        data.PreviousIncome = await BuildIncomeStatementAsync(fiscalYear.Previous.StartDate, fiscalYear.Previous.EndDate);

        data.CurrentEquity = await BuildChangesInEquityAsync(fiscalYear, false);
        data.PreviousEquity = await BuildChangesInEquityAsync(fiscalYear.Previous, true);

        data.CurrentCashFlow = await BuildCashFlowAsync(fiscalYear.StartDate, fiscalYear.EndDate);
        data.PreviousCashFlow = await BuildCashFlowAsync(fiscalYear.Previous.StartDate, fiscalYear.Previous.EndDate);

        data.Notes = BuildAccountingNotes();
        return data;
    }

    public List<FiscalYear> GetAvailableFiscalYears()
    {
        var now = DateTime.Now;
        var currentStart = now.Month >= 7 ? now.Year : now.Year - 1;
        var years = new List<FiscalYear>();
        for (var i = 0; i < 5; i++)
        {
            years.Add(new FiscalYear { StartYear = currentStart - i, EndYear = currentStart - i + 1 });
        }
        return years;
    }

    private async Task<BalanceSheetData> BuildBalanceSheetAsync(DateTime from, DateTime to)
    {
        var bills = await context.Bills
            .Where(b => b.BillDate >= from && b.BillDate <= to && b.Status != "Cancelled")
            .ToListAsync();

        var purchases = await context.Purchases
            .Include(p => p.Items)
            .Where(p => p.PurchaseDate >= from && p.PurchaseDate <= to)
            .ToListAsync();

        var expenses = await context.Expenses
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var payables = await context.Payables
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .ToListAsync();

        var duePayments = await context.DuePayments
            .Include(d => d.Bill)
            .Where(d => d.PaymentDate >= from && d.PaymentDate <= to)
            .ToListAsync();

        var medicines = await context.Medicines
            .Where(m => m.IsActive)
            .ToListAsync();

        var totalSales = bills.Where(b => b.Status != "Cancelled").Sum(b => b.FinalAmount);
        var totalCollected = bills.Where(b => b.Status != "Cancelled").Sum(b => b.PaidAmount);
        var totalDue = bills.Where(b => b.Status != "Cancelled").Sum(b => b.DueAmount);
        var totalPurchases = purchases.Sum(p => p.TotalAmount);
        var totalPaidToSuppliers = payables.Sum(p => p.PaidAmount);
        var totalDueToSuppliers = payables.Sum(p => p.PendingAmount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var totalDueCollected = duePayments.Sum(d => d.AmountPaid);

        var inventoryValue = medicines.Sum(m => m.StockQuantity * m.PurchasePrice);
        var cashFromSales = totalCollected;
        var cashReceivables = totalDueCollected;

        var revenue = totalSales;
        var cost = purchases.SelectMany(p => p.Items)
            .Sum(i => i.Quantity * i.PurchasePrice);
        var netProfit = revenue - cost - totalExpenses;

        return new BalanceSheetData
        {
            CashAndBank = cashFromSales - totalPaidToSuppliers - totalExpenses + cashReceivables,
            AccountsReceivable = totalDue,
            Inventory = inventoryValue,
            OtherCurrentAssets = 0,

            AccountsPayable = totalDueToSuppliers,
            DueToSuppliers = totalDueToSuppliers,
            OtherCurrentLiabilities = 0,

            OwnerEquity = Math.Max(0, cost + totalExpenses),
            RetainedEarnings = 0,
            NetProfitLoss = netProfit
        };
    }

    private async Task<IncomeStatementData> BuildIncomeStatementAsync(DateTime from, DateTime to)
    {
        var bills = await context.Bills
            .Where(b => b.BillDate >= from && b.BillDate <= to && b.Status != "Cancelled")
            .ToListAsync();

        var billIds = bills.Select(b => b.Id).ToList();

        var saleItems = await context.SaleItems
            .Where(s => billIds.Contains(s.BillId))
            .ToListAsync();

        var medicineIds = saleItems.Select(s => s.MedicineId).Distinct().ToList();
        var medicines = await context.Medicines
            .Where(m => medicineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.PurchasePrice);

        var purchases = await context.Purchases
            .Include(p => p.Items)
            .Where(p => p.PurchaseDate >= from && p.PurchaseDate <= to)
            .ToListAsync();

        var expenses = await context.Expenses
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var expenseByCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var revenue = bills.Sum(b => b.FinalAmount);
        var discountTotal = bills.Sum(b => b.DiscountAmount);
        var cost = saleItems.Sum(s => medicines.GetValueOrDefault(s.MedicineId) * s.Quantity);

        decimal salaryExpense = expenseByCategory.GetValueOrDefault("Salary", 0);
        decimal rentExpense = expenseByCategory.GetValueOrDefault("Rent", 0);
        decimal utilitiesExpense = expenseByCategory.GetValueOrDefault("Utilities", 0)
            + expenseByCategory.GetValueOrDefault("Electricity", 0)
            + expenseByCategory.GetValueOrDefault("Internet", 0);
        decimal otherExpenses = expenses
            .Where(e => e.Category != "Salary" && e.Category != "Rent"
                && e.Category != "Utilities" && e.Category != "Electricity" && e.Category != "Internet")
            .Sum(e => e.Amount);

        return new IncomeStatementData
        {
            SalesRevenue = revenue,
            SalesReturns = discountTotal,
            CostOfGoodsSold = cost,
            SalaryExpense = salaryExpense,
            RentExpense = rentExpense,
            UtilitiesExpense = utilitiesExpense,
            OtherExpenses = otherExpenses,
            BillCount = bills.Count,
            PurchaseCount = purchases.Count
        };
    }

    private async Task<ChangesInEquityData> BuildChangesInEquityAsync(FiscalYear fiscalYear, bool isPrevious)
    {
        var income = await BuildIncomeStatementAsync(fiscalYear.StartDate, fiscalYear.EndDate);
        var profit = income.NetProfit;

        var allPreviousYearsProfit = 0m;
        if (isPrevious)
        {
            var prevPrev = fiscalYear.Previous;
            var prevIncome = await BuildIncomeStatementAsync(prevPrev.StartDate, prevPrev.EndDate);
            allPreviousYearsProfit = prevIncome.NetProfit;
        }

        return new ChangesInEquityData
        {
            OpeningBalance = isPrevious ? allPreviousYearsProfit : 0,
            AdditionalCapital = 0,
            NetProfit = profit,
            OwnerDrawings = 0
        };
    }

    private async Task<CashFlowData> BuildCashFlowAsync(DateTime from, DateTime to)
    {
        var bills = await context.Bills
            .Where(b => b.BillDate >= from && b.BillDate <= to && b.Status != "Cancelled")
            .ToListAsync();

        var duePayments = await context.DuePayments
            .Include(d => d.Bill)
            .Where(d => d.PaymentDate >= from && d.PaymentDate <= to)
            .ToListAsync();

        var payables = await context.Payables
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .ToListAsync();

        var expenses = await context.Expenses
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var cashFromSales = bills.Sum(b => b.PaidAmount);
        var cashFromReceivables = duePayments.Sum(d => d.AmountPaid);
        var cashPaidToSuppliers = payables.Sum(p => p.PaidAmount);
        var cashPaidForExpenses = expenses.Sum(e => e.Amount);
        var cashPaidForSalaries = expenses.Where(e => e.Category == "Salary").Sum(e => e.Amount);

        var netOperating = (cashFromSales + cashFromReceivables)
            - (cashPaidToSuppliers + cashPaidForExpenses + cashPaidForSalaries);

        var previousEnd = from.AddDays(-1);
        var prevCashBank = await context.Bills
            .Where(b => b.BillDate < from && b.Status != "Cancelled")
            .SumAsync(b => b.PaidAmount);

        return new CashFlowData
        {
            CashFromSales = cashFromSales,
            CashFromReceivables = cashFromReceivables,
            CashPaidToSuppliers = cashPaidToSuppliers,
            CashPaidForExpenses = cashPaidForExpenses - cashPaidForSalaries,
            CashPaidForSalaries = cashPaidForSalaries,
            OpeningCashBalance = Math.Max(0, prevCashBank - cashPaidToSuppliers),
            ClosingCashBalance = Math.Max(0, prevCashBank + netOperating)
        };
    }

    private static List<AccountingNote> BuildAccountingNotes()
    {
        return
        [
            new() { NoteNumber = 1, Title = "Basis of Preparation",
                Content = "These financial statements have been prepared on a going concern basis under the historical cost convention. They comply with the applicable accounting standards and the provisions of the prevailing commercial laws of Nepal." },
            new() { NoteNumber = 2, Title = "Revenue Recognition",
                Content = "Revenue from sales is recognized at the point of sale when goods are delivered to customers. Sales are recorded net of returns, discounts, and trade allowances." },
            new() { NoteNumber = 3, Title = "Inventory Valuation",
                Content = "Inventories of medicines and pharmaceutical products are valued at the lower of cost (weighted average) and net realizable value. Cost includes purchase price and other directly attributable costs." },
            new() { NoteNumber = 4, Title = "Depreciation",
                Content = "Fixed assets are depreciated on a straight-line basis over their estimated useful lives. Depreciation commences when the asset is available for use." },
            new() { NoteNumber = 5, Title = "Cash and Cash Equivalents",
                Content = "Cash and cash equivalents include cash in hand, cash at bank, and short-term highly liquid investments with maturity of three months or less." },
            new() { NoteNumber = 6, Title = "Accounts Receivable",
                Content = "Trade receivables are recognized at their invoiced amount. An allowance for expected credit losses is made based on a review of outstanding balances at the reporting date." },
            new() { NoteNumber = 7, Title = "Accounts Payable",
                Content = "Trade payables are recognized at their invoiced amount for goods and services received but not yet paid at the reporting date." },
            new() { NoteNumber = 8, Title = "Significant Judgments",
                Content = "The management has exercised judgment in applying the company's accounting policies. The areas involving significant judgment include the valuation of inventory, estimation of expected credit losses, and determination of useful lives of fixed assets." }
        ];
    }
}
