using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Helpers;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class BillingService(ApplicationDbContext context, PdfGenerator pdfGenerator, BackupService backupService)
{
    public async Task<Bill> CreateBillAsync(CreateBillRequest request, string? staffId, string? staffName)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("At least one medicine is required.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var medicineIds = request.Items.Select(i => i.MedicineId).Distinct().ToList();
            var medicines = await context.Medicines
                .Where(m => medicineIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            // Load FEFO batches: earliest expiry first, then by Id for stable ordering
            var batchesByMedicine = await context.InventoryBatches
                .Where(b => medicineIds.Contains(b.MedicineId) && b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                .ThenBy(b => b.ExpiryDate)
                .ThenBy(b => b.Id)
                .ToListAsync();

            var batchLookup = batchesByMedicine
                .GroupBy(b => b.MedicineId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in request.Items)
            {
                if (!medicines.TryGetValue(item.MedicineId, out var medicine))
                    throw new InvalidOperationException($"Medicine ID {item.MedicineId} not found.");

                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"Invalid quantity for {medicine.Name}.");

                if (medicine.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {medicine.Name}. Available: {medicine.StockQuantity}");
            }

            var totalAmount = request.Items.Sum(i => i.Rate * i.Quantity);
            var itemDiscount = request.Items.Sum(i => i.DiscountAmount);
            var subtotalAfterDiscount = totalAmount - itemDiscount - request.DiscountAmount;

            if (subtotalAfterDiscount < 0)
                throw new InvalidOperationException("Final amount cannot be negative.");

            var company = await context.CompanyProfiles.SingleOrDefaultAsync();
            var taxRate = company?.TaxRate ?? 0;
            var taxInclusive = company?.TaxInclusive ?? false;
            decimal taxAmount = 0;

            if (taxRate > 0 && !taxInclusive)
                taxAmount = Math.Round(subtotalAfterDiscount * taxRate / 100m, 2);

            var finalAmount = subtotalAfterDiscount + taxAmount;

            var bill = new Bill
            {
                BillNumber = await GenerateBillNumberAsync(),
                BillDate = DateTime.Now,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim(),
                TotalAmount = totalAmount,
                DiscountAmount = request.DiscountAmount + itemDiscount,
                TaxAmount = taxAmount,
                FinalAmount = finalAmount,
                PaymentMethod = request.PaymentMethod,
                StaffId = staffId,
                StaffName = staffName
            };

            ApplyPaymentLogic(bill);

            foreach (var item in request.Items)
            {
                var medicine = medicines[item.MedicineId];

                // FEFO: deduct stock from batches earliest-expiry first
                int remaining = item.Quantity;
                InventoryBatch? firstBatch = null;

                if (batchLookup.TryGetValue(item.MedicineId, out var batches))
                {
                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;
                        firstBatch ??= batch;
                        int deduct = Math.Min(batch.Quantity, remaining);
                        batch.Quantity -= deduct;
                        remaining -= deduct;
                    }
                }

                // Deduct from medicine stock total (remaining covers edge case where no batches exist)
                medicine.StockQuantity -= item.Quantity;

                bill.SaleItems.Add(new SaleItem
                {
                    MedicineId  = item.MedicineId,
                    MedicineName = medicine.Name,
                    MRP          = firstBatch?.PurchasePrice > 0 ? medicine.MRP : medicine.MRP,
                    ExpiryDate   = firstBatch?.ExpiryDate ?? medicine.ExpiryDate,
                    BatchId      = firstBatch?.Id,
                    BatchNumber  = firstBatch?.BatchNumber ?? medicine.BatchNumber,
                    Rate         = item.Rate,
                    Quantity     = item.Quantity,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount  = item.DiscountAmount,
                    Amount       = (item.Rate * item.Quantity) - item.DiscountAmount
                });

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MedicineId      = medicine.Id,
                    TransactionType = TransactionTypes.Sale,
                    QuantityChange  = -item.Quantity,
                    Reference       = $"Bill {bill.BillNumber}"
                });
            }

            context.Bills.Add(bill);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                var billCount = await context.Bills.CountAsync();
                if (billCount > 0 && billCount % 10 == 0)
                {
                    await backupService.CreateBackupAsync();
                }
            }
            catch
            {
                // Silently ignore backup errors so billing doesn't fail after transaction commit
            }

            return bill;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static void ApplyPaymentLogic(Bill bill)
    {
        if (bill.PaymentMethod == PaymentMethods.Due)
        {
            bill.PaidAmount   = 0;
            bill.DueAmount    = bill.FinalAmount;
            bill.PaymentStatus = PaymentStatuses.Due;
        }
        else
        {
            bill.PaidAmount   = bill.FinalAmount;
            bill.DueAmount    = 0;
            bill.PaymentStatus = PaymentStatuses.Paid;
        }
    }

    public async Task<Bill?> GetByIdAsync(int id) =>
        await context.Bills
            .Include(b => b.SaleItems)
            .Include(b => b.DuePayments)
            .SingleOrDefaultAsync(b => b.Id == id);

    public async Task<byte[]> GenerateInvoicePdfAsync(int billId)
    {
        var bill = await GetByIdAsync(billId)
            ?? throw new InvalidOperationException("Bill not found.");

        var company = await context.CompanyProfiles.SingleOrDefaultAsync()
            ?? new CompanyProfile { Name = "PrimeRx" };

        return pdfGenerator.GenerateInvoice(bill, company);
    }

    private async Task<string> GenerateBillNumberAsync()
    {
        var today = DateTime.Now.Date;
        var prefix = $"BILL-{today:yyyyMMdd}-";
        var count = await context.Bills.CountAsync(b => b.BillDate.Date == today);
        return $"{prefix}{(count + 1):D4}";
    }
}
