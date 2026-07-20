namespace PrimeRx.Models.ViewModels;

public class FiscalYear
{
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public string Label => $"{StartYear}/{(EndYear % 100):D2}";
    public DateTime StartDate => new(StartYear, 7, 1);
    public DateTime EndDate => new(EndYear, 6, 30, 23, 59, 59);
    public FiscalYear Previous => new() { StartYear = StartYear - 1, EndYear = EndYear - 1 };
}

public class AuditFinancialData
{
    public FiscalYear CurrentYear { get; set; } = new();
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyPAN { get; set; } = string.Empty;
    public string? CompanyLogoPath { get; set; }

    public BalanceSheetData CurrentBalanceSheet { get; set; } = new();
    public BalanceSheetData PreviousBalanceSheet { get; set; } = new();
    public IncomeStatementData CurrentIncome { get; set; } = new();
    public IncomeStatementData PreviousIncome { get; set; } = new();
    public ChangesInEquityData CurrentEquity { get; set; } = new();
    public ChangesInEquityData PreviousEquity { get; set; } = new();
    public CashFlowData CurrentCashFlow { get; set; } = new();
    public CashFlowData PreviousCashFlow { get; set; } = new();
    public List<AccountingNote> Notes { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class BalanceSheetData
{
    public decimal TotalCurrentAssets { get; set; }
    public decimal TotalFixedAssets { get; set; }
    public decimal TotalAssets => TotalCurrentAssets + TotalFixedAssets;

    public decimal CashAndBank { get; set; }
    public decimal AccountsReceivable { get; set; }
    public decimal Inventory { get; set; }
    public decimal OtherCurrentAssets { get; set; }

    public decimal TotalCurrentLiabilities { get; set; }
    public decimal TotalLongTermLiabilities { get; set; }
    public decimal TotalLiabilities => TotalCurrentLiabilities + TotalLongTermLiabilities;

    public decimal AccountsPayable { get; set; }
    public decimal DueToSuppliers { get; set; }
    public decimal OtherCurrentLiabilities { get; set; }

    public decimal OwnerEquity { get; set; }
    public decimal RetainedEarnings { get; set; }
    public decimal NetProfitLoss { get; set; }
    public decimal TotalEquity => OwnerEquity + RetainedEarnings + NetProfitLoss;
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
}

public class IncomeStatementData
{
    public decimal SalesRevenue { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal NetSales => SalesRevenue - SalesReturns;
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => NetSales - CostOfGoodsSold;

    public decimal SalaryExpense { get; set; }
    public decimal RentExpense { get; set; }
    public decimal UtilitiesExpense { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal TotalOperatingExpenses => SalaryExpense + RentExpense + UtilitiesExpense + OtherExpenses;

    public decimal OperatingProfit => GrossProfit - TotalOperatingExpenses;
    public decimal NetProfit => OperatingProfit;

    public int BillCount { get; set; }
    public int PurchaseCount { get; set; }
}

public class ChangesInEquityData
{
    public decimal OpeningBalance { get; set; }
    public decimal AdditionalCapital { get; set; }
    public decimal NetProfit { get; set; }
    public decimal OwnerDrawings { get; set; }
    public decimal ClosingBalance => OpeningBalance + AdditionalCapital + NetProfit - OwnerDrawings;
}

public class CashFlowData
{
    public decimal CashFromSales { get; set; }
    public decimal CashFromReceivables { get; set; }
    public decimal TotalOperatingInflow => CashFromSales + CashFromReceivables;

    public decimal CashPaidToSuppliers { get; set; }
    public decimal CashPaidForExpenses { get; set; }
    public decimal CashPaidForSalaries { get; set; }
    public decimal TotalOperatingOutflow => CashPaidToSuppliers + CashPaidForExpenses + CashPaidForSalaries;

    public decimal NetOperatingCashFlow => TotalOperatingInflow - TotalOperatingOutflow;
    public decimal OpeningCashBalance { get; set; }
    public decimal ClosingCashBalance { get; set; }
}

public class AccountingNote
{
    public int NoteNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class VendorReportData
{
    public string VendorName { get; set; } = string.Empty;
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
    public string? SelectedVendor { get; set; }
    public List<VendorTransactionRow> Transactions { get; set; } = [];
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDueBalance { get; set; }
    public List<VendorSummaryRow> VendorSummaries { get; set; } = [];
    public List<string> AllVendorNames { get; set; } = [];
}

public class VendorTransactionRow
{
    public string VendorName { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string? GRNNumber { get; set; }
    public DateTime Date { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueBalance { get; set; }
    public List<VendorMedicineDetail> Medicines { get; set; } = [];
    public string PaymentStatus { get; set; } = string.Empty;
}

public class VendorMedicineDetail
{
    public string MedicineName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal MRP { get; set; }
}

public class VendorSummaryRow
{
    public string VendorName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
}
