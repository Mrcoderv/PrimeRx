using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Helpers;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class BillingService(ApplicationDbContext context, PdfGenerator pdfGenerator)
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

            var company = await context.CompanyProfiles.FirstOrDefaultAsync();
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
                CustomerPhone = request.CustomerPhone.Trim(),
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
                medicine.StockQuantity -= item.Quantity;

                bill.SaleItems.Add(new SaleItem
                {
                    MedicineId = item.MedicineId,
                    MedicineName = medicine.Name,
                    MRP = medicine.MRP,
                    ExpiryDate = medicine.ExpiryDate,
                    Rate = item.Rate,
                    Quantity = item.Quantity,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    Amount = (item.Rate * item.Quantity) - item.DiscountAmount
                });

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MedicineId = medicine.Id,
                    TransactionType = TransactionTypes.Sale,
                    QuantityChange = -item.Quantity,
                    Reference = $"Bill {bill.BillNumber}"
                });
            }

            context.Bills.Add(bill);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

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
            bill.PaidAmount = 0;
            bill.DueAmount = bill.FinalAmount;
            bill.PaymentStatus = PaymentStatuses.Due;
        }
        else
        {
            bill.PaidAmount = bill.FinalAmount;
            bill.DueAmount = 0;
            bill.PaymentStatus = PaymentStatuses.Paid;
        }
    }

    public async Task<Bill?> GetByIdAsync(int id) =>
        await context.Bills
            .Include(b => b.SaleItems)
            .Include(b => b.DuePayments)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<byte[]> GenerateInvoicePdfAsync(int billId)
    {
        var bill = await GetByIdAsync(billId)
            ?? throw new InvalidOperationException("Bill not found.");

        var company = await context.CompanyProfiles.FirstOrDefaultAsync()
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
