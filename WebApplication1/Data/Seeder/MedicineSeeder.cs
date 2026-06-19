using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data.Seeder;

public static class MedicineSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Medicines.AnyAsync())
            return;

        var medicines = new List<Medicine>
        {
            new() { Name = "Paracetamol 500mg", GenericName = "Paracetamol", Manufacturer = "Cipla", MRP = 25, PurchasePrice = 15, StockQuantity = 500, Category = "Analgesic", ExpiryDate = DateTime.Now.AddMonths(18) },
            new() { Name = "Ibuprofen 400mg", GenericName = "Ibuprofen", Manufacturer = "Sun Pharma", MRP = 35, PurchasePrice = 22, StockQuantity = 300, Category = "Analgesic", ExpiryDate = DateTime.Now.AddMonths(24) },
            new() { Name = "Amoxicillin 500mg", GenericName = "Amoxicillin", Manufacturer = "GSK", MRP = 120, PurchasePrice = 80, StockQuantity = 200, Category = "Antibiotic", ExpiryDate = DateTime.Now.AddMonths(12) },
            new() { Name = "Azithromycin 500mg", GenericName = "Azithromycin", Manufacturer = "Cipla", MRP = 95, PurchasePrice = 60, StockQuantity = 150, Category = "Antibiotic", ExpiryDate = DateTime.Now.AddMonths(15) },
            new() { Name = "Cetirizine 10mg", GenericName = "Cetirizine", Manufacturer = "Dr. Reddy's", MRP = 18, PurchasePrice = 10, StockQuantity = 400, Category = "Antihistamine", ExpiryDate = DateTime.Now.AddMonths(20) },
            new() { Name = "Omeprazole 20mg", GenericName = "Omeprazole", Manufacturer = "Torrent", MRP = 45, PurchasePrice = 28, StockQuantity = 250, Category = "Antacid", ExpiryDate = DateTime.Now.AddMonths(18) },
            new() { Name = "Metformin 500mg", GenericName = "Metformin", Manufacturer = "USV", MRP = 30, PurchasePrice = 18, StockQuantity = 350, Category = "Antidiabetic", ExpiryDate = DateTime.Now.AddMonths(24) },
            new() { Name = "Amlodipine 5mg", GenericName = "Amlodipine", Manufacturer = "Lupin", MRP = 40, PurchasePrice = 25, StockQuantity = 280, Category = "Antihypertensive", ExpiryDate = DateTime.Now.AddMonths(22) },
            new() { Name = "Atorvastatin 10mg", GenericName = "Atorvastatin", Manufacturer = "Pfizer", MRP = 85, PurchasePrice = 55, StockQuantity = 180, Category = "Statin", ExpiryDate = DateTime.Now.AddMonths(16) },
            new() { Name = "Salbutamol Inhaler", GenericName = "Salbutamol", Manufacturer = "Cipla", MRP = 180, PurchasePrice = 120, StockQuantity = 80, Category = "Bronchodilator", ExpiryDate = DateTime.Now.AddMonths(12) },
            new() { Name = "ORS Powder", GenericName = "Oral Rehydration Salts", Manufacturer = "FDC", MRP = 22, PurchasePrice = 12, StockQuantity = 600, Category = "Electrolyte", ExpiryDate = DateTime.Now.AddMonths(30) },
            new() { Name = "Vitamin D3 60K", GenericName = "Cholecalciferol", Manufacturer = "Abbott", MRP = 110, PurchasePrice = 70, StockQuantity = 120, Category = "Vitamin", ExpiryDate = DateTime.Now.AddMonths(18) },
            new() { Name = "Dolo 650", GenericName = "Paracetamol", Manufacturer = "Micro Labs", MRP = 32, PurchasePrice = 20, StockQuantity = 450, Category = "Analgesic", ExpiryDate = DateTime.Now.AddMonths(20) },
            new() { Name = "Crocin Advance", GenericName = "Paracetamol", Manufacturer = "GSK", MRP = 38, PurchasePrice = 24, StockQuantity = 320, Category = "Analgesic", ExpiryDate = DateTime.Now.AddMonths(18) },
            new() { Name = "Betadine Solution 100ml", GenericName = "Povidone Iodine", Manufacturer = "Win-Medicare", MRP = 95, PurchasePrice = 60, StockQuantity = 90, Category = "Antiseptic", ExpiryDate = DateTime.Now.AddMonths(24) },
        };

        context.Medicines.AddRange(medicines);
        await context.SaveChangesAsync();
    }
}
