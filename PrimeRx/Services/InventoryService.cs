using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class InventoryService(ApplicationDbContext context)
{
    static InventoryService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    public async Task<List<Medicine>> GetAllAsync(string? search = null, bool includeInactive = false)
    {
        var query = context.Medicines.AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.GenericName != null && m.GenericName.ToLower().Contains(term)) ||
                (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(term)));
        }

        return await query.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<List<MedicineSearchResult>> SearchMedicinesAsync(string term, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(term))
            return [];

        var search = term.Trim().ToLower();

        return await context.Medicines
            .Where(m => m.IsActive && m.StockQuantity > 0 &&
                (m.Name.ToLower().Contains(search) ||
                 (m.GenericName != null && m.GenericName.ToLower().Contains(search))))
            .OrderBy(m => m.Name)
            .Take(limit)
            .Select(m => new MedicineSearchResult
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Manufacturer = m.Manufacturer,
                FormType = m.FormType,
                MRP = m.MRP,
                PurchasePrice = m.PurchasePrice,
                StockQuantity = m.StockQuantity,
                DiscountPercent = m.DiscountPercent
            })
            .ToListAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id) =>
        await context.Medicines.FindAsync(id);

    public async Task<Medicine> CreateAsync(Medicine medicine)
    {
        if (medicine.MRP <= 0 && medicine.PurchasePrice > 0)
            medicine.MRP = CalculateMrp(medicine.PurchasePrice, await GetDefaultMarginPercentAsync());

        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();

        if (medicine.StockQuantity > 0)
        {
            context.InventoryBatches.Add(new InventoryBatch
            {
                MedicineId = medicine.Id,
                BatchNumber = string.IsNullOrWhiteSpace(medicine.BatchNumber) ? "INITIAL" : medicine.BatchNumber.Trim(),
                Quantity = medicine.StockQuantity,
                PurchasePrice = medicine.PurchasePrice,
                PurchaseSource = medicine.PurchaseSource ?? string.Empty,
                ExpiryDate = medicine.ExpiryDate
            });

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                MedicineId = medicine.Id,
                TransactionType = TransactionTypes.Purchase,
                QuantityChange = medicine.StockQuantity,
                Reference = "Initial stock"
            });

            await context.SaveChangesAsync();
        }

        return medicine;
    }

    /// <summary>Default markup margin (%) used to derive MRP from purchase price; falls back to 16% if no profile exists.</summary>
    public async Task<decimal> GetDefaultMarginPercentAsync()
    {
        var profile = await context.CompanyProfiles.AsNoTracking().SingleOrDefaultAsync();
        return profile?.DefaultDiscountMarginPercent ?? 16m;
    }

    public static decimal CalculateMrp(decimal purchasePrice, decimal marginPercent) =>
        Math.Round(purchasePrice * (1 + marginPercent / 100m), 2);

    public async Task UpdateAsync(Medicine medicine)
    {
        context.Medicines.Update(medicine);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var medicine = await context.Medicines.FindAsync(id);
        if (medicine is null) return;

        medicine.IsActive = false;
        await context.SaveChangesAsync();
    }

    public async Task RecordPurchaseAsync(PurchaseEntryRequest request)
    {
        if (request.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var medicine = await context.Medicines.FindAsync(request.MedicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        var totalUnits = request.Quantity + Math.Max(0, request.FreeQuantity);
        medicine.StockQuantity += totalUnits;

        if (request.PurchasePrice is > 0)
        {
            medicine.PurchasePrice = request.PurchasePrice.Value;
            medicine.MRP = CalculateMrp(medicine.PurchasePrice, await GetDefaultMarginPercentAsync());
        }

        var batchNumber = request.BatchNumber?.Trim();
        var purchaseSource = request.PurchaseSource?.Trim();

        if (!string.IsNullOrWhiteSpace(batchNumber))
            medicine.BatchNumber = batchNumber;
        if (!string.IsNullOrWhiteSpace(purchaseSource))
            medicine.PurchaseSource = purchaseSource;
        if (request.ExpiryDate.HasValue)
            medicine.ExpiryDate = request.ExpiryDate;

        context.InventoryBatches.Add(new InventoryBatch
        {
            MedicineId = medicine.Id,
            BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? "N/A" : batchNumber,
            Quantity = totalUnits,
            PurchasePrice = request.PurchasePrice ?? medicine.PurchasePrice,
            PurchaseSource = purchaseSource ?? request.Reference?.Trim() ?? string.Empty,
            ExpiryDate = request.ExpiryDate
        });

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            MedicineId = medicine.Id,
            TransactionType = TransactionTypes.Purchase,
            QuantityChange = totalUnits,
            Reference = BuildPurchaseReference(request, totalUnits)
        });

        await context.SaveChangesAsync();
    }

    /// <summary>Reduces stock for a purchase return (e.g. expired/damaged goods sent back to a supplier) and finds the best-matching batch to shrink.</summary>
    public async Task ReturnToSupplierAsync(int medicineId, int quantity, string? batchNumber, string reference)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Return quantity must be greater than zero.");

        var medicine = await context.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        if (medicine.StockQuantity < quantity)
            throw new InvalidOperationException($"Cannot return {quantity} units of '{medicine.Name}' — only {medicine.StockQuantity} in stock.");

        medicine.StockQuantity -= quantity;

        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            var batch = await context.InventoryBatches
                .Where(b => b.MedicineId == medicineId && b.BatchNumber == batchNumber.Trim())
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            if (batch is not null)
                batch.Quantity = Math.Max(0, batch.Quantity - quantity);
        }

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            MedicineId = medicine.Id,
            TransactionType = TransactionTypes.Return,
            QuantityChange = -quantity,
            Reference = reference
        });

        await context.SaveChangesAsync();
    }

    public async Task<List<InventoryBatch>> GetBatchesAsync(int? medicineId = null)
    {
        var query = context.InventoryBatches.Include(b => b.Medicine).AsQueryable();
        if (medicineId.HasValue)
            query = query.Where(b => b.MedicineId == medicineId.Value);

        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    private static string BuildPurchaseReference(PurchaseEntryRequest request, int totalUnits)
    {
        var parts = new List<string> { $"Purchase +{totalUnits}" };
        if (request.FreeQuantity > 0)
            parts.Add($"incl. {request.FreeQuantity} free");
        if (!string.IsNullOrWhiteSpace(request.BatchNumber))
            parts.Add($"Batch {request.BatchNumber.Trim()}");
        var source = request.PurchaseSource?.Trim() ?? request.Reference?.Trim();
        if (!string.IsNullOrWhiteSpace(source))
            parts.Add(source);
        return string.Join(" • ", parts);
    }

    public async Task AdjustStockAsync(StockAdjustmentRequest request)
    {
        var medicine = await context.Medicines.FindAsync(request.MedicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        var newStock = medicine.StockQuantity + request.QuantityChange;
        if (newStock < 0)
            throw new InvalidOperationException("Stock cannot go below zero.");

        medicine.StockQuantity = newStock;

        // Adjust the most recent batch for this medicine
        var latestBatch = await context.InventoryBatches
            .Where(b => b.MedicineId == medicine.Id)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestBatch is not null)
        {
            var newBatchQty = latestBatch.Quantity + request.QuantityChange;
            latestBatch.Quantity = Math.Max(0, newBatchQty);
        }
        else if (request.QuantityChange > 0)
        {
            context.InventoryBatches.Add(new InventoryBatch
            {
                MedicineId = medicine.Id,
                BatchNumber = "ADJ",
                Quantity = request.QuantityChange,
                PurchasePrice = medicine.PurchasePrice,
                PurchaseSource = request.Reference ?? "Stock Adjustment",
                ExpiryDate = medicine.ExpiryDate
            });
        }

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            MedicineId = medicine.Id,
            TransactionType = TransactionTypes.Adjustment,
            QuantityChange = request.QuantityChange,
            Reference = request.Reference ?? $"Adjustment {request.QuantityChange:+0;-#}"
        });

        await context.SaveChangesAsync();
    }

    public async Task<List<InventoryTransaction>> GetTransactionHistoryAsync(int? medicineId = null, int limit = 100)
    {
        var query = context.InventoryTransactions
            .Include(t => t.Medicine)
            .AsQueryable();

        if (medicineId.HasValue)
            query = query.Where(t => t.MedicineId == medicineId.Value);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Medicine>> GetLowStockAsync() =>
        await context.Medicines
            .Where(m => m.IsActive && m.StockQuantity <= m.LowStockThreshold)
            .OrderBy(m => m.StockQuantity)
            .ToListAsync();

    public async Task<List<Medicine>> GetExpiringSoonAsync(int days = 90) =>
        await context.Medicines
            .Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate <= DateTime.Now.AddDays(days))
            .OrderBy(m => m.ExpiryDate)
            .ToListAsync();

    /// <summary>
    /// Records a stock exchange (Aaicho Paicho) transfer to another pharmacy.
    /// Tracks quantity only, no price involved in the exchange.
    /// </summary>
        public async Task<List<MedicineMaster>> SearchMasterForAutoFillAsync(string term, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(term)) return [];
            var t = term.Trim().ToLower();
            return await context.MedicineMasters
                .Where(m => m.IsActive && (
                    m.GenericName.ToLower().Contains(t) ||
                    (m.BrandName != null && m.BrandName.ToLower().Contains(t))))
                .OrderBy(m => m.GenericName)
                .Take(limit)
                .ToListAsync();
        }

        public byte[] ExportInventoryStockToExcel()
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Inventory Stock");
            sheet.Cells[1, 1].Value = "Medicine";
            sheet.Cells[1, 2].Value = "Generic Name";
            sheet.Cells[1, 3].Value = "Manufacturer";
            sheet.Cells[1, 4].Value = "Form";
            sheet.Cells[1, 5].Value = "Batch #";
            sheet.Cells[1, 6].Value = "Expiry Date";
            sheet.Cells[1, 7].Value = "Quantity";
            sheet.Cells[1, 8].Value = "Purchase Price";
            sheet.Cells[1, 9].Value = "MRP";
            sheet.Cells[1, 10].Value = "Category";
            sheet.Cells[1, 11].Value = "Rack";

            using var range = sheet.Cells[1, 1, 1, 11];
            range.Style.Font.Bold = true;

            var medicines = context.Medicines.Where(m => m.IsActive).OrderBy(m => m.Name).ToList();
            var medicineMap = medicines.ToDictionary(m => m.Id, m => m);
            var allBatches = context.InventoryBatches.Where(b => b.Quantity > 0)
                .OrderBy(b => b.MedicineId).ThenBy(b => b.ExpiryDate).ToList();
            var batchesByMed = allBatches.GroupBy(b => b.MedicineId).ToDictionary(g => g.Key, g => g.ToList());

            var row = 2;
            foreach (var m in medicines)
            {
                if (batchesByMed.TryGetValue(m.Id, out var batches))
                {
                    foreach (var b in batches)
                    {
                        sheet.Cells[row, 1].Value = m.Name;
                        sheet.Cells[row, 2].Value = m.GenericName;
                        sheet.Cells[row, 3].Value = m.Manufacturer;
                        sheet.Cells[row, 4].Value = m.FormType;
                        sheet.Cells[row, 5].Value = b.BatchNumber;
                        sheet.Cells[row, 6].Value = b.ExpiryDate?.ToString("dd-MM-yyyy");
                        sheet.Cells[row, 7].Value = b.Quantity;
                        sheet.Cells[row, 8].Value = b.PurchasePrice;
                        sheet.Cells[row, 9].Value = m.MRP;
                        sheet.Cells[row, 10].Value = m.Category;
                        sheet.Cells[row, 11].Value = "";
                        row++;
                    }
                }
                else
                {
                    sheet.Cells[row, 1].Value = m.Name;
                    sheet.Cells[row, 2].Value = m.GenericName;
                    sheet.Cells[row, 3].Value = m.Manufacturer;
                    sheet.Cells[row, 4].Value = m.FormType;
                    sheet.Cells[row, 5].Value = m.BatchNumber;
                    sheet.Cells[row, 6].Value = m.ExpiryDate?.ToString("dd-MM-yyyy");
                    sheet.Cells[row, 7].Value = m.StockQuantity;
                    sheet.Cells[row, 8].Value = m.PurchasePrice;
                    sheet.Cells[row, 9].Value = m.MRP;
                    sheet.Cells[row, 10].Value = m.Category;
                    sheet.Cells[row, 11].Value = "";
                    row++;
                }
            }

            for (var c = 1; c <= 11; c++) sheet.Column(c).AutoFit();
            return package.GetAsByteArray();
        }

        public async Task ExchangeStockAsync(int medicineId, int quantity, string otherPharmacy, string? reference)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Exchange quantity must be greater than zero.");

        var medicine = await context.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        if (medicine.StockQuantity < quantity)
            throw new InvalidOperationException($"Cannot exchange {quantity} units of '{medicine.Name}' — only {medicine.StockQuantity} in stock.");

        medicine.StockQuantity -= quantity;

        var latestBatch = await context.InventoryBatches
            .Where(b => b.MedicineId == medicine.Id)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestBatch is not null)
            latestBatch.Quantity = Math.Max(0, latestBatch.Quantity - quantity);

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            MedicineId = medicine.Id,
            TransactionType = TransactionTypes.Exchange,
            QuantityChange = -quantity,
            Reference = $"Exchange to {otherPharmacy}" + (string.IsNullOrWhiteSpace(reference) ? "" : $" • {reference}")
        });

        await context.SaveChangesAsync();
    }

    public async Task<List<Medicine>> GetExpiringMedicinesAsync(int daysThreshold = 90)
    {
        var today = DateTime.Today;
        return await context.Medicines
            .Where(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value <= today.AddDays(daysThreshold))
            .OrderBy(m => m.ExpiryDate)  // Ascending expiry = soonest first
            .ToListAsync();
    }
}
