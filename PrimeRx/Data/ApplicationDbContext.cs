using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Models;

namespace PrimeRx.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<DuePayment> DuePayments => Set<DuePayment>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Medicine>()
            .HasIndex(m => m.Name);

        modelBuilder.Entity<Bill>()
            .HasIndex(b => b.CustomerPhone);

        modelBuilder.Entity<Bill>()
            .HasIndex(b => b.BillNumber)
            .IsUnique();

        modelBuilder.Entity<SaleItem>()
            .HasOne(s => s.Bill)
            .WithMany(b => b.SaleItems)
            .HasForeignKey(s => s.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DuePayment>()
            .HasOne(d => d.Bill)
            .WithMany(b => b.DuePayments)
            .HasForeignKey(d => d.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Medicine)
            .WithMany()
            .HasForeignKey(t => t.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryBatch>()
            .HasOne(b => b.Medicine)
            .WithMany()
            .HasForeignKey(b => b.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

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
