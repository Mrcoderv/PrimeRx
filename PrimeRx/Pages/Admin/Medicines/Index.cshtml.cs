using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Medicines;

public class IndexModel(InventoryService inventoryService, ApplicationDbContext context) : PageModel
{
    public List<Medicine> Medicines { get; set; } = [];
    public string? Search { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public bool ShowImportModal { get; set; }

    public async Task OnGetAsync(string? search, string? message)
    {
        Search = search;
        Message = message;
        Medicines = await inventoryService.GetAllAsync(search, includeInactive: true);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await inventoryService.DeleteAsync(id);
        return RedirectToPage(new { message = "Medicine deactivated." });
    }

    public IActionResult OnGetTemplateAsync()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Medicines");

        string[] headers = ["Name", "GenericName", "Manufacturer", "FormType",
                             "MRP", "PurchasePrice", "StockQuantity", "Category",
                             "LowStockThreshold", "DiscountPercent"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
            ws.Cells[1, c + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(37, 99, 235));
            ws.Cells[1, c + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        ws.Cells[2, 1].Value = "Paracetamol 500mg";
        ws.Cells[2, 2].Value = "Paracetamol";
        ws.Cells[2, 3].Value = "ABC Pharma";
        ws.Cells[2, 4].Value = "Tablet";
        ws.Cells[2, 5].Value = 25.00;
        ws.Cells[2, 6].Value = 18.00;
        ws.Cells[2, 7].Value = 100;
        ws.Cells[2, 8].Value = "Analgesic";
        ws.Cells[2, 9].Value = 10;
        ws.Cells[2, 10].Value = 0;

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        var bytes = package.GetAsByteArray();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MedicineImportTemplate.xlsx");
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var medicines = await inventoryService.GetAllAsync(null, includeInactive: true);

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Medicines");

        // Header row
        string[] headers = ["Name", "GenericName", "Manufacturer", "FormType",
                             "MRP", "PurchasePrice", "StockQuantity", "Category",
                             "LowStockThreshold", "DiscountPercent", "IsActive"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
        }

        // Data rows
        int row = 2;
        foreach (var m in medicines)
        {
            ws.Cells[row, 1].Value  = m.Name;
            ws.Cells[row, 2].Value  = m.GenericName;
            ws.Cells[row, 3].Value  = m.Manufacturer;
            ws.Cells[row, 4].Value  = m.FormType;
            ws.Cells[row, 5].Value  = m.MRP;
            ws.Cells[row, 6].Value  = m.PurchasePrice;
            ws.Cells[row, 7].Value  = m.StockQuantity;
            ws.Cells[row, 8].Value  = m.Category;
            ws.Cells[row, 9].Value  = m.LowStockThreshold;
            ws.Cells[row, 10].Value = m.DiscountPercent;
            ws.Cells[row, 11].Value = m.IsActive ? "Yes" : "No";
            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        var bytes = package.GetAsByteArray();
        var fileName = $"MedicineCatalog_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? importFile)
    {
        if (importFile == null || importFile.Length == 0)
        {
            IsError = true;
            Message = "Please select a valid Excel file.";
            ShowImportModal = true;
            Medicines = await inventoryService.GetAllAsync(null, includeInactive: true);
            return Page();
        }

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int added = 0, updated = 0, skipped = 0;

            using var stream = new MemoryStream();
            await importFile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                IsError = true;
                Message = "No worksheet found in the Excel file.";
                ShowImportModal = true;
                Medicines = await inventoryService.GetAllAsync(null, includeInactive: true);
                return Page();
            }

            // Map header columns
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= ws.Dimension?.Columns; col++)
            {
                var hdr = ws.Cells[1, col].Text.Trim();
                if (!string.IsNullOrEmpty(hdr))
                    headers[hdr] = col;
            }

            int GetCol(string name) => headers.TryGetValue(name, out var c) ? c : -1;
            string Cell(int row, string col)
            {
                int c = GetCol(col);
                return c > 0 ? ws.Cells[row, c].Text.Trim() : string.Empty;
            }

            var allMedicines = context.Medicines.ToDictionary(
                m => (m.Name.ToLower().Trim() + "|" + (m.Manufacturer ?? "").ToLower().Trim()), m => m);

            var rows = ws.Dimension?.Rows ?? 1;
            for (int row = 2; row <= rows; row++)
            {
                var name = Cell(row, "Name");
                if (string.IsNullOrWhiteSpace(name)) { skipped++; continue; }

                decimal.TryParse(Cell(row, "MRP"), out var mrp);
                decimal.TryParse(Cell(row, "PurchasePrice"), out var purchasePrice);
                int.TryParse(Cell(row, "StockQuantity"), out var stock);
                int.TryParse(Cell(row, "LowStockThreshold"), out var threshold);
                decimal.TryParse(Cell(row, "DiscountPercent"), out var discount);

                var mfrImport = Cell(row, "Manufacturer");
                var key = name.ToLower().Trim() + "|" + mfrImport.ToLower().Trim();
                if (allMedicines.TryGetValue(key, out var existing))
                {
                    // Update existing
                    var gen  = Cell(row, "GenericName");
                    var mfr  = Cell(row, "Manufacturer");
                    var form = Cell(row, "FormType");
                    var cat  = Cell(row, "Category");
                    if (!string.IsNullOrEmpty(gen))  existing.GenericName  = gen;
                    if (!string.IsNullOrEmpty(mfr))  { existing.Manufacturer = mfr; }
                    if (!string.IsNullOrEmpty(form)) existing.FormType     = form;
                    if (!string.IsNullOrEmpty(cat))  existing.Category     = cat;
                    if (mrp > 0) existing.MRP = mrp;
                    if (purchasePrice > 0) existing.PurchasePrice = purchasePrice;
                    if (stock > 0) existing.StockQuantity = stock;
                    if (threshold > 0) existing.LowStockThreshold = threshold;
                    if (discount > 0) existing.DiscountPercent = discount;
                    updated++;
                }
                else
                {
                    // Add new
                    var medicine = new Medicine
                    {
                        Name              = name,
                        GenericName       = Cell(row, "GenericName")  is { Length: > 0 } g  ? g  : null,
                        Manufacturer      = Cell(row, "Manufacturer") is { Length: > 0 } m2 ? m2 : null,
                        FormType          = Cell(row, "FormType")     is { Length: > 0 } ft ? ft : null,
                        Category          = Cell(row, "Category")     is { Length: > 0 } c  ? c  : null,
                        MRP               = mrp,
                        PurchasePrice     = purchasePrice,
                        StockQuantity     = stock,
                        LowStockThreshold = threshold > 0 ? threshold : 10,
                        DiscountPercent   = discount,
                        IsActive          = true
                    };
                    context.Medicines.Add(medicine);
                    allMedicines[key] = medicine;
                    added++;
                }
            }

            await context.SaveChangesAsync();
            Message = $"Import complete: {added} added, {updated} updated, {skipped} skipped.";
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = $"Import failed: {ex.Message}";
            ShowImportModal = true;
        }

        Medicines = await inventoryService.GetAllAsync(null, includeInactive: true);
        return Page();
    }
}
