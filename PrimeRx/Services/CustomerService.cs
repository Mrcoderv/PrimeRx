using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

/// <summary>
/// Service for managing customer records, purchase history, and retention metrics.
/// </summary>
public class CustomerService(ApplicationDbContext context)
{
    /// <summary>
    /// Get all active customers with optional search by name or phone.
    /// </summary>
    public async Task<List<Customer>> GetAllAsync(string? search = null, bool includeInactive = false)
    {
        var query = context.Customers.AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                    (c.Phone != null && c.Phone.Contains(searchTerm)));
        }

        return await query.OrderByDescending(c => c.LastPurchaseDate)
                         .ThenBy(c => c.Name)
                         .ToListAsync();
    }

    /// <summary>
    /// Get a customer by ID with full purchase history.
    /// </summary>
    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await context.Customers
            .Include(c => c.Bills)
            .ThenInclude(b => b.SaleItems)
            .SingleOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Get or create a customer by phone number.
    /// Used during billing to quickly link/create a customer.
    /// </summary>
    public async Task<Customer> GetOrCreateAsync(string? phone, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            // Return a transient guest customer for anonymous sales
            return new Customer { Name = name ?? "Guest", Phone = null };
        }

        var existing = await context.Customers.SingleOrDefaultAsync(c => c.Phone == phone && c.IsActive);
        if (existing != null)
            return existing;

        // Create new customer
        var newCustomer = new Customer
        {
            Name = name ?? phone,
            Phone = phone,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        context.Customers.Add(newCustomer);
        await context.SaveChangesAsync();
        return newCustomer;
    }

    /// <summary>
    /// Create a new customer record.
    /// </summary>
    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.CreatedAt = DateTime.Now;
        customer.IsActive = true;
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    /// <summary>
    /// Update an existing customer record.
    /// </summary>
    public async Task UpdateAsync(Customer customer)
    {
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Soft-delete a customer (mark inactive).
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var customer = await context.Customers.FindAsync(id);
        if (customer != null)
        {
            customer.IsActive = false;
            context.Customers.Update(customer);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get purchase history for a customer.
    /// </summary>
    public async Task<List<Bill>> GetPurchaseHistoryAsync(int customerId, int limit = 50)
    {
        return await context.Bills
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BillDate)
            .Take(limit)
            .Include(b => b.SaleItems)
            .ToListAsync();
    }

    /// <summary>
    /// Update customer retention metrics after a bill is created.
    /// Should be called by BillingService after a successful sale.
    /// </summary>
    public async Task UpdateRetentionAsync(int customerId, decimal billAmount)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null)
            return;

        customer.TotalSpent += billAmount;
        customer.LastPurchaseDate = DateTime.Now;

        // Award loyalty points: 1 point per ₹100 spent (configurable)
        int pointsEarned = (int)(billAmount / 100);
        customer.LoyaltyPoints += pointsEarned;

        context.Customers.Update(customer);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Manually add loyalty points to a customer (for promotions, rewards, etc.).
    /// </summary>
    public async Task AddLoyaltyPointsAsync(int customerId, int points)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            customer.LoyaltyPoints += points;
            context.Customers.Update(customer);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Redeem loyalty points (e.g., apply discount).
    /// Returns true if sufficient points; false otherwise.
    /// </summary>
    public async Task<bool> RedeemLoyaltyPointsAsync(int customerId, int pointsToRedeem)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null || customer.LoyaltyPoints < pointsToRedeem)
            return false;

        customer.LoyaltyPoints -= pointsToRedeem;
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get top customers by total spending (for analytics/retention).
    /// </summary>
    public async Task<List<Customer>> GetTopCustomersAsync(int count = 10)
    {
        return await context.Customers
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalSpent)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get customers who haven't purchased in a specified number of days (churn risk).
    /// </summary>
    public async Task<List<Customer>> GetChurnRiskCustomersAsync(int daysSinceLastPurchase = 30)
    {
        var cutoffDate = DateTime.Now.AddDays(-daysSinceLastPurchase);
        return await context.Customers
            .Where(c => c.IsActive && (c.LastPurchaseDate == null || c.LastPurchaseDate < cutoffDate))
            .OrderByDescending(c => c.TotalSpent)  // High-value customers first
            .ToListAsync();
    }
}
