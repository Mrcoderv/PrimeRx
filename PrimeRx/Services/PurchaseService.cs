using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class PurchaseService(ApplicationDbContext context, InventoryService inventoryService, BackupService backupService)
{
    public async Task<List<Purchase>> GetAllAsync(int limit = 100)
    {
        return await context.Purchases
            .Include(p => p.Items)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Purchase?> GetByIdAsync(int id)
    {
        return await context.Purchases
            .Include(p => p.Items)
                .ThenInclude(i => i.Medicine)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Purchase>> GetBySupplierAsync(string supplierName)
    {
        return await context.Purchases
            .Include(p => p.Items)
            .Where(p => p.SupplierName.ToLower().Contains(supplierName.ToLower()))
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();
    }

    public async Task<Purchase> CreateAsync(PurchaseCreateRequest request, string? createdBy)
    {
        if (!request.Items.Any())
            throw new InvalidOperationException("At least one item is required.");

        var margin = await inventoryService.GetDefaultMarginPercentAsync();

        var purchase = new Purchase
        {
            PurchaseDate = request.PurchaseDate,
            SupplierName = request.SupplierName.Trim(),
            SupplierPhone = request.SupplierPhone?.Trim(),
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        decimal total = 0;

        foreach (var line in request.Items)
        {
            var medicine = await ResolveMedicineAsync(line.MedicineId);

            if (line.Quantity <= 0)
                throw new InvalidOperationException($"Quantity must be > 0 for '{medicine.Name}'.");

            var mrp = line.MRP > 0 ? line.MRP : InventoryService.CalculateMrp(line.PurchasePrice, margin);

            purchase.Items.Add(new PurchaseItem
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                Quantity = line.Quantity,
                FreeQuantity = Math.Max(0, line.FreeQuantity),
                PurchasePrice = line.PurchasePrice,
                DiscountPercent = line.DiscountPercent,
                MRP = mrp,
                ConversionCharge = line.ConversionCharge,
                BatchNumber = line.BatchNumber?.Trim(),
                ExpiryDate = line.ExpiryDate
            });

            total += Math.Round(line.Quantity * line.PurchasePrice * (1 - line.DiscountPercent / 100m), 2) + line.ConversionCharge;

            // Update stock and batch record
            await inventoryService.RecordPurchaseAsync(new PurchaseEntryRequest
            {
                MedicineId = medicine.Id,
                Quantity = line.Quantity,
                FreeQuantity = Math.Max(0, line.FreeQuantity),
                PurchasePrice = line.PurchasePrice > 0 ? line.PurchasePrice : null,
                BatchNumber = line.BatchNumber,
                PurchaseSource = request.SupplierName,
                ExpiryDate = line.ExpiryDate,
                Reference = request.InvoiceNumber
            });
        }

        purchase.TotalAmount = total;

        context.Purchases.Add(purchase);

        if (request.PaymentType == "Credit" && total > 0)
        {
            int creditDays = request.CreditDays
                ?? await GetSupplierCreditDaysAsync(request.SupplierName)
                ?? 30;

            var creditApplied = await ApplyAvailableCreditAsync(request.SupplierName, total);

            context.Payables.Add(new Payable
            {
                SupplierName = request.SupplierName.Trim(),
                InvoiceNo = request.InvoiceNumber?.Trim(),
                Amount = total,
                PaidAmount = creditApplied,
                DueDate = DateTime.Today.AddDays(creditDays),
                Status = creditApplied >= total ? PayableStatus.Paid : PayableStatus.Pending,
                Description = creditApplied > 0
                    ? $"Auto-created from purchase on {request.PurchaseDate:dd MMM yyyy} — {creditApplied.ToRs()} adjusted from available credit notes"
                    : $"Auto-created from purchase on {request.PurchaseDate:dd MMM yyyy}",
                CreatedAt = DateTime.Now
            });
        }

        await context.SaveChangesAsync();

        try
        {
            await backupService.CreateBackupAsync();
        }
        catch
        {
            // Silently ignore backup errors so purchase operation doesn't fail
        }

        return purchase;
    }

    /// <summary>
    /// Resolves a medicine ID to a <see cref="Medicine"/> record.
    /// If <paramref name="medicineId"/> is negative, it represents a <see cref="MedicineMaster"/> entry
    /// that hasn't been added to local inventory yet; this method creates a <see cref="Medicine"/> from
    /// the master catalog on the fly and returns the newly-created record.
    /// </summary>
    private async Task<Medicine> ResolveMedicineAsync(int medicineId)
    {
        if (medicineId > 0)
        {
            return await context.Medicines.FindAsync(medicineId)
                ?? throw new InvalidOperationException($"Medicine ID {medicineId} not found.");
        }

        var master = await context.MedicineMasters.FindAsync(-medicineId)
            ?? throw new InvalidOperationException($"MedicineMaster ID {-medicineId} not found.");

        var medicine = new Medicine
        {
            Name = master.DisplayName,
            GenericName = master.GenericName,
            Manufacturer = master.Manufacturer,
            FormType = master.Form,
            Category = master.Category,
            IsActive = true,
            LowStockThreshold = 10
        };

        await inventoryService.CreateAsync(medicine);
        return medicine;
    }

    /// <summary>Applies available (unused) credit notes for a supplier against a new purchase total, oldest first. Returns the amount applied.</summary>
    private async Task<decimal> ApplyAvailableCreditAsync(string supplierName, decimal total)
    {
        var notes = await context.CreditNotes
            .Where(c => c.SupplierName.ToLower() == supplierName.Trim().ToLower() && c.Status != CreditNoteStatus.FullyUsed)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        decimal remaining = total;
        decimal applied = 0;

        foreach (var note in notes)
        {
            if (remaining <= 0) break;

            var take = Math.Min(note.AvailableAmount, remaining);
            if (take <= 0) continue;

            note.UsedAmount += take;
            note.Status = note.AvailableAmount <= 0 ? CreditNoteStatus.FullyUsed : CreditNoteStatus.PartiallyUsed;

            applied += take;
            remaining -= take;
        }

        return applied;
    }

    public async Task<decimal> GetAvailableCreditAsync(string supplierName)
    {
        return await context.CreditNotes
            .Where(c => c.SupplierName.ToLower() == supplierName.Trim().ToLower() && c.Status != CreditNoteStatus.FullyUsed)
            .SumAsync(c => c.Amount - c.UsedAmount);
    }

    public async Task UpdateAsync(int id, PurchaseCreateRequest request, string? updatedBy)
    {
        var existing = await context.Purchases
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Purchase not found.");

        if (!request.Items.Any())
            throw new InvalidOperationException("At least one item is required.");

        var margin = await inventoryService.GetDefaultMarginPercentAsync();

        // Reverse original stock for items no longer in the edit
        var existingItemIds = existing.Items.Select(i => i.Id).ToHashSet();
        var newItemIds = request.Items.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();
        var removedItems = existing.Items.Where(i => !newItemIds.Contains(i.Id)).ToList();

        foreach (var removed in removedItems)
        {
            await inventoryService.AdjustStockAsync(new StockAdjustmentRequest
            {
                MedicineId = removed.MedicineId,
                QuantityChange = -removed.Quantity,
                Reference = $"Purchase #{id} edit — removed item"
            });
        }

        // Update header fields
        existing.PurchaseDate = request.PurchaseDate;
        existing.SupplierName = request.SupplierName.Trim();
        existing.SupplierPhone = request.SupplierPhone?.Trim();
        existing.InvoiceNumber = request.InvoiceNumber?.Trim();
        existing.Notes = request.Notes?.Trim();

        decimal total = 0;
        var updatedItems = new List<PurchaseItem>();

        foreach (var line in request.Items)
        {
            var medicine = await ResolveMedicineAsync(line.MedicineId);

            if (line.Quantity <= 0)
                throw new InvalidOperationException($"Quantity must be > 0 for '{medicine.Name}'.");

            var mrp = line.MRP > 0 ? line.MRP : InventoryService.CalculateMrp(line.PurchasePrice, margin);

            if (line.Id > 0 && existingItemIds.Contains(line.Id))
            {
                // Existing item: adjust stock difference
                var orig = existing.Items.First(i => i.Id == line.Id);
                int diff = line.Quantity - orig.Quantity;
                if (diff != 0)
                {
                    await inventoryService.AdjustStockAsync(new StockAdjustmentRequest
                    {
                        MedicineId = medicine.Id,
                        QuantityChange = diff,
                        Reference = $"Purchase #{id} edit — qty change"
                    });
                }

                orig.Quantity = line.Quantity;
                orig.PurchasePrice = line.PurchasePrice;
                orig.DiscountPercent = line.DiscountPercent;
                orig.FreeQuantity = Math.Max(0, line.FreeQuantity);
                orig.MRP = mrp;
                orig.ConversionCharge = line.ConversionCharge;
                orig.BatchNumber = line.BatchNumber?.Trim();
                orig.ExpiryDate = line.ExpiryDate;
                updatedItems.Add(orig);
            }
            else
            {
                // New item
                var newItem = new PurchaseItem
                {
                    MedicineId = medicine.Id,
                    MedicineName = medicine.Name,
                    Quantity = line.Quantity,
                    FreeQuantity = Math.Max(0, line.FreeQuantity),
                    PurchasePrice = line.PurchasePrice,
                    DiscountPercent = line.DiscountPercent,
                    MRP = mrp,
                    ConversionCharge = line.ConversionCharge,
                    BatchNumber = line.BatchNumber?.Trim(),
                    ExpiryDate = line.ExpiryDate
                };
                updatedItems.Add(newItem);

                await inventoryService.RecordPurchaseAsync(new PurchaseEntryRequest
                {
                    MedicineId = medicine.Id,
                    Quantity = line.Quantity,
                    FreeQuantity = Math.Max(0, line.FreeQuantity),
                    PurchasePrice = line.PurchasePrice > 0 ? line.PurchasePrice : null,
                    BatchNumber = line.BatchNumber,
                    PurchaseSource = request.SupplierName,
                    ExpiryDate = line.ExpiryDate
                });
            }

            total += Math.Round(line.Quantity * line.PurchasePrice * (1 - line.DiscountPercent / 100m), 2) + line.ConversionCharge;
        }

        // Remove deleted items from DB
        context.PurchaseItems.RemoveRange(removedItems);

        // Replace items collection
        existing.Items.Clear();
        foreach (var item in updatedItems)
            existing.Items.Add(item);

        existing.TotalAmount = total;
        await context.SaveChangesAsync();

        try
        {
            await backupService.CreateBackupAsync();
        }
        catch
        {
            // Silently ignore backup errors so purchase operation doesn't fail
        }
    }

    public async Task DeleteAsync(int id)
    {
        var purchase = await context.Purchases
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Purchase not found.");

        // Reverse stock
        foreach (var item in purchase.Items)
        {
            await inventoryService.AdjustStockAsync(new StockAdjustmentRequest
            {
                MedicineId = item.MedicineId,
                QuantityChange = -item.Quantity,
                Reference = $"Purchase #{id} deleted"
            });
        }

        context.Purchases.Remove(purchase);
        await context.SaveChangesAsync();

        try
        {
            await backupService.CreateBackupAsync();
        }
        catch
        {
            // Silently ignore backup errors so purchase operation doesn't fail
        }
    }

    public async Task<List<string>> GetSuppliersAsync()
    {
        var fromSupplierTable = await context.Suppliers
            .Where(s => s.IsActive)
            .Select(s => s.Name)
            .ToListAsync();

        var fromPurchases = await context.Purchases
            .Select(p => p.SupplierName)
            .Distinct()
            .ToListAsync();

        return fromSupplierTable
            .Union(fromPurchases)
            .OrderBy(s => s)
            .ToList();
    }

    public async Task<int?> GetSupplierCreditDaysAsync(string supplierName)
    {
        var supplier = await context.Suppliers
            .Where(s => s.Name == supplierName && s.IsActive)
            .SingleOrDefaultAsync();
        return supplier?.CreditDays;
    }
}
