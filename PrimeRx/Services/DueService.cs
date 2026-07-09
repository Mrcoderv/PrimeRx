using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;

namespace PrimeRx.Services;

public class DueService(ApplicationDbContext context, NotificationService notificationService)
{
    public async Task<List<Bill>> GetDueBillsAsync(string? search = null)
    {
        var query = context.Bills
            .Include(b => b.DuePayments)
            .Where(b => b.DueAmount > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.CustomerName.ToLower().Contains(term) ||
                (b.CustomerPhone != null && b.CustomerPhone.Contains(term)) ||
                b.BillNumber.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();
    }

    public async Task<Bill> RecordPaymentAsync(RecordDuePaymentRequest request)
    {
        var bill = await context.Bills
            .Include(b => b.DuePayments)
            .SingleOrDefaultAsync(b => b.Id == request.BillId)
            ?? throw new InvalidOperationException("Bill not found.");

        if (bill.DueAmount <= 0)
            throw new InvalidOperationException("This bill has no outstanding due amount.");

        if (request.AmountPaid <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        if (request.AmountPaid > bill.DueAmount)
            throw new InvalidOperationException($"Payment exceeds due amount of {bill.DueAmount:C}.");

        var payment = new DuePayment
        {
            BillId = bill.Id,
            AmountPaid = request.AmountPaid,
            PaymentMethod = request.PaymentMethod,
            Remarks = request.Remarks,
            PaymentDate = DateTime.Now
        };

        bill.PaidAmount += request.AmountPaid;
        bill.DueAmount -= request.AmountPaid;

        if (bill.DueAmount <= 0)
        {
            bill.DueAmount = 0;
            bill.PaymentStatus = PaymentStatuses.Paid;
        }
        else
        {
            bill.PaymentStatus = PaymentStatuses.PartiallyPaid;
        }

        context.DuePayments.Add(payment);
        await context.SaveChangesAsync();

        // If fully paid, mark related notifications as action completed
        if (bill.DueAmount <= 0)
        {
            await notificationService.MarkActionCompletedAsync("Bill", bill.Id);
        }

        return bill;
    }

    public async Task<List<DuePayment>> GetPaymentHistoryAsync(int billId) =>
        await context.DuePayments
            .Where(d => d.BillId == billId)
            .OrderByDescending(d => d.PaymentDate)
            .ToListAsync();
}
