using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Models;

namespace PrimeRx.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineForm> MedicineForms => Set<MedicineForm>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<DuePayment> DuePayments => Set<DuePayment>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Payable> Payables => Set<Payable>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MedicineMaster> MedicineMasters => Set<MedicineMaster>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Medicine indices
        modelBuilder.Entity<Medicine>()
            .HasIndex(m => m.Name);

        // MedicineForm indices and relationships
        modelBuilder.Entity<MedicineForm>()
            .HasOne(mf => mf.Medicine)
            .WithMany()
            .HasForeignKey(mf => mf.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicineForm>()
            .HasIndex(mf => new { mf.MedicineId, mf.FormType });

        // Customer indices
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Phone);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Name);

        // Bill relationships and indices
        modelBuilder.Entity<Bill>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bills)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Bill>()
            .HasIndex(b => b.CustomerPhone);

        modelBuilder.Entity<Bill>()
            .HasIndex(b => b.BillNumber)
            .IsUnique();

        modelBuilder.Entity<Bill>()
            .HasIndex(b => b.BillDate);

        // SaleItem relationships
        modelBuilder.Entity<SaleItem>()
            .HasOne(s => s.Bill)
            .WithMany(b => b.SaleItems)
            .HasForeignKey(s => s.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleItem>()
            .HasOne(s => s.Medicine)
            .WithMany()
            .HasForeignKey(s => s.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SaleItem>()
            .HasOne(s => s.Batch)
            .WithMany()
            .HasForeignKey(s => s.BatchId)
            .OnDelete(DeleteBehavior.SetNull);

        // DuePayment relationships
        modelBuilder.Entity<DuePayment>()
            .HasOne(d => d.Bill)
            .WithMany(b => b.DuePayments)
            .HasForeignKey(d => d.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        // InventoryTransaction relationships
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Medicine)
            .WithMany()
            .HasForeignKey(t => t.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // InventoryBatch relationships
        modelBuilder.Entity<InventoryBatch>()
            .HasOne(b => b.Medicine)
            .WithMany()
            .HasForeignKey(b => b.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryBatch>()
            .HasOne(b => b.MedicineForm)
            .WithMany(mf => mf.Batches)
            .HasForeignKey(b => b.MedicineFormId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryBatch>()
            .HasIndex(b => new { b.MedicineId, b.ExpiryDate });

        // Expense indices
        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.ExpenseDate);

        // PurchaseItem relationships
        modelBuilder.Entity<PurchaseItem>()
            .HasOne(pi => pi.Purchase)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseItem>()
            .HasOne(pi => pi.Medicine)
            .WithMany()
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase indices
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.PurchaseDate);

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.SupplierName);

        // Supplier indices
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name);

        // PurchaseReturnItem relationships
        modelBuilder.Entity<PurchaseReturnItem>()
            .HasOne(pri => pri.PurchaseReturn)
            .WithMany(pr => pr.Items)
            .HasForeignKey(pri => pri.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseReturnItem>()
            .HasOne(pri => pri.Medicine)
            .WithMany()
            .HasForeignKey(pri => pri.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // PurchaseReturn relationships and indices
        modelBuilder.Entity<PurchaseReturn>()
            .HasOne(pr => pr.Purchase)
            .WithMany()
            .HasForeignKey(pr => pr.PurchaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseReturn>()
            .HasIndex(pr => pr.SupplierName);

        modelBuilder.Entity<PurchaseReturn>()
            .HasIndex(pr => pr.ReturnDate);

        // CreditNote relationships
        modelBuilder.Entity<CreditNote>()
            .HasOne(c => c.PurchaseReturn)
            .WithMany()
            .HasForeignKey(c => c.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CreditNote>()
            .HasIndex(c => c.SupplierName);

        // AuditLog indices
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityType, a.EntityId });

        // MedicineMaster
        modelBuilder.Entity<MedicineMaster>()
            .HasIndex(m => m.GenericName);

        modelBuilder.Entity<MedicineMaster>()
            .HasIndex(m => m.BrandName);

        // Set decimal precision globally
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
