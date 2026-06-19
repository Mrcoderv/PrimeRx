using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Services;

public class InventoryService(ApplicationDbContext context)
{
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
                MRP = m.MRP,
                StockQuantity = m.StockQuantity
            })
            .ToListAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id) =>
        await context.Medicines.FindAsync(id);

    public async Task<Medicine> CreateAsync(Medicine medicine)
    {
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();
        return medicine;
    }

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
        var medicine = await context.Medicines.FindAsync(request.MedicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        medicine.StockQuantity += request.Quantity;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            MedicineId = medicine.Id,
            TransactionType = TransactionTypes.Purchase,
            QuantityChange = request.Quantity,
            Reference = request.Reference ?? $"Purchase +{request.Quantity}"
        });

        await context.SaveChangesAsync();
    }

    public async Task AdjustStockAsync(StockAdjustmentRequest request)
    {
        var medicine = await context.Medicines.FindAsync(request.MedicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        var newStock = medicine.StockQuantity + request.QuantityChange;
        if (newStock < 0)
            throw new InvalidOperationException("Stock cannot go below zero.");

        medicine.StockQuantity = newStock;

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
}
