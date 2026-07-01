using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class PurchaseReturnService(ApplicationDbContext context, InventoryService inventoryService)
{
    public async Task<List<PurchaseReturn>> GetAllAsync(int limit = 100)
    {
        return await context.PurchaseReturns
            .Include(r => r.Items)
            .OrderByDescending(r => r.ReturnDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<PurchaseReturn?> GetByIdAsync(int id)
    {
        return await context.PurchaseReturns
            .Include(r => r.Items)
                .ThenInclude(i => i.Medicine)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<PurchaseReturn>> GetBySupplierAsync(string supplierName)
    {
        return await context.PurchaseReturns
            .Include(r => r.Items)
            .Where(r => r.SupplierName.ToLower().Contains(supplierName.ToLower()))
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync();
    }

    public async Task<PurchaseReturn> CreateAsync(PurchaseReturnCreateRequest request, string? createdBy)
    {
        if (!request.Items.Any())
            throw new InvalidOperationException("At least one item is required for the return.");

        var purchaseReturn = new PurchaseReturn
        {
            ReturnDate = request.ReturnDate,
            SupplierName = request.SupplierName.Trim(),
            PurchaseId = request.PurchaseId,
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            Reason = request.Reason,
            Notes = request.Notes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        decimal total = 0;

        foreach (var line in request.Items)
        {
            var medicine = await context.Medicines.FindAsync(line.MedicineId)
                ?? throw new InvalidOperationException($"Medicine ID {line.MedicineId} not found.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException($"Return quantity must be > 0 for '{medicine.Name}'.");

            purchaseReturn.Items.Add(new PurchaseReturnItem
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                BatchNumber = line.BatchNumber?.Trim(),
                Quantity = line.Quantity,
                PurchasePrice = line.PurchasePrice
            });

            total += line.Quantity * line.PurchasePrice;

            await inventoryService.ReturnToSupplierAsync(
                medicine.Id,
                line.Quantity,
                line.BatchNumber,
                $"Return to {request.SupplierName.Trim()} — {PurchaseReturnReasons.Display(request.Reason)}");
        }

        purchaseReturn.TotalAmount = total;
        context.PurchaseReturns.Add(purchaseReturn);
        await context.SaveChangesAsync();

        if (total > 0)
        {
            context.CreditNotes.Add(new CreditNote
            {
                SupplierName = purchaseReturn.SupplierName,
                PurchaseReturnId = purchaseReturn.Id,
                Amount = total,
                UsedAmount = 0,
                Status = CreditNoteStatus.Available,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        return purchaseReturn;
    }

    public async Task<List<CreditNote>> GetCreditNotesAsync(string? supplierName = null)
    {
        var query = context.CreditNotes.Include(c => c.PurchaseReturn).AsQueryable();
        if (!string.IsNullOrWhiteSpace(supplierName))
            query = query.Where(c => c.SupplierName.ToLower() == supplierName.Trim().ToLower());

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }
}

public class PurchaseReturnLineItem
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Amount => Quantity * PurchasePrice;
}

public class PurchaseReturnCreateRequest
{
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public string SupplierName { get; set; } = string.Empty;
    public int? PurchaseId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string Reason { get; set; } = PurchaseReturnReasons.Other;
    public string? Notes { get; set; }
    public List<PurchaseReturnLineItem> Items { get; set; } = [];
}
