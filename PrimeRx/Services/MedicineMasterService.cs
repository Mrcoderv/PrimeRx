using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

public class MedicineMasterService(ApplicationDbContext context)
{
    static MedicineMasterService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<List<MedicineMaster>> GetAllAsync(string? search = null, string? letter = null, bool includeInactive = false)
    {
        var query = context.MedicineMasters.AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(letter) && letter.Length == 1)
        {
            var l = letter.Trim().ToLower()[0];
            query = query.Where(m => m.GenericName.ToLower().StartsWith(l.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.GenericName.ToLower().Contains(term) ||
                (m.BrandName != null && m.BrandName.ToLower().Contains(term)) ||
                (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(term)) ||
                (m.Category != null && m.Category.ToLower().Contains(term)));
        }

        return await query.OrderBy(m => m.GenericName).ToListAsync();
    }

    public async Task<int> GetCountAsync() =>
        await context.MedicineMasters.CountAsync(m => m.IsActive);

    public async Task<MedicineMaster?> GetByIdAsync(int id) =>
        await context.MedicineMasters.FindAsync(id);

    public async Task<HashSet<string>> GetGenericNamesWithStockAsync()
    {
        var names = await context.Medicines
            .Where(m => m.StockQuantity > 0)
            .Select(m => m.GenericName)
            .Distinct()
            .ToListAsync();
        return [.. names.Where(n => n != null).Select(n => n!.ToLower())];
    }

    public async Task<List<MedicineMaster>> SearchAsync(string term, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(term)) return [];
        var t = term.Trim().ToLower();
        return await context.MedicineMasters
            .Where(m => m.IsActive && (
                m.GenericName.ToLower().Contains(t) ||
                (m.BrandName != null && m.BrandName.ToLower().Contains(t))))
            .OrderBy(m => m.GenericName)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<MedicineMaster> CreateAsync(MedicineMaster master)
    {
        master.CreatedAt = DateTime.UtcNow;
        master.UpdatedAt = DateTime.UtcNow;
        context.MedicineMasters.Add(master);
        await context.SaveChangesAsync();
        return master;
    }

    public async Task<MedicineMaster> UpdateAsync(MedicineMaster master)
    {
        master.UpdatedAt = DateTime.UtcNow;
        context.MedicineMasters.Update(master);
        await context.SaveChangesAsync();
        return master;
    }

    public async Task DeleteAsync(int id)
    {
        var master = await context.MedicineMasters.FindAsync(id);
        if (master != null)
        {
            master.IsActive = false;
            master.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task SeedFromNepalDatabaseAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return;

        var hasData = await context.MedicineMasters.AnyAsync();
        if (hasData) return;

        await using var fs = System.IO.File.OpenRead(filePath);
        await ImportFromExcelAsync(fs, updateExisting: false);
    }

    public async Task<ImportResult> ImportFromExcelAsync(Stream stream, bool updateExisting = true)
    {
        using var package = new ExcelPackage(stream);
        var sheet = package.Workbook.Worksheets[0];
        if (sheet == null)
            return new ImportResult { Errors = ["No worksheet found."] };

        var result = new ImportResult();

        var header = sheet.Cells[1, 1].Text?.Trim().ToLower() ?? "";
        var isNepalFormat = header is "s.no" or "s.no";

        var row = 2;
        while (sheet.Cells[row, isNepalFormat ? 2 : 1].Value != null)
        {
            string? genericName;
            string? brandName;
            string? manufacturer;
            string? category;
            string? strength;
            string? form;

            string? hsnCode = null;
            string? rack = null;
            string? unit = null;

            if (isNepalFormat)
            {
                // Nepal 5K columns: S.No, Generic Name, Brand Name, Manufacturer, Company Type, Therapeutic Category, Strength/Form
                genericName = sheet.Cells[row, 2].Text?.Trim();
                brandName = sheet.Cells[row, 3].Text?.Trim();
                manufacturer = sheet.Cells[row, 4].Text?.Trim();
                category = sheet.Cells[row, 6].Text?.Trim();
                var strengthForm = sheet.Cells[row, 7].Text?.Trim();
                (form, strength) = ParseStrengthForm(strengthForm);
            }
            else
            {
                genericName = sheet.Cells[row, 1].Text?.Trim();
                brandName = sheet.Cells[row, 2].Text?.Trim();
                manufacturer = sheet.Cells[row, 3].Text?.Trim();
                form = sheet.Cells[row, 4].Text?.Trim();
                strength = sheet.Cells[row, 5].Text?.Trim();
                unit = sheet.Cells[row, 6].Text?.Trim();
                category = sheet.Cells[row, 7].Text?.Trim();
                hsnCode = sheet.Cells[row, 8].Text?.Trim();
                rack = sheet.Cells[row, 9].Text?.Trim();
            }

            if (string.IsNullOrWhiteSpace(genericName))
            {
                result.Skipped++;
                row++;
                continue;
            }

            var existing = await context.MedicineMasters
                .FirstOrDefaultAsync(m =>
                    m.GenericName.ToLower() == genericName.ToLower() &&
                    ((m.BrandName == null && brandName == null) || (m.BrandName != null && brandName != null && m.BrandName.ToLower() == brandName.ToLower())) &&
                    ((m.Manufacturer == null && manufacturer == null) || (m.Manufacturer != null && manufacturer != null && m.Manufacturer.ToLower() == manufacturer.ToLower())) &&
                    ((m.Form == null && form == null) || (m.Form != null && form != null && m.Form.ToLower() == form.ToLower())) &&
                    ((m.Strength == null && strength == null) || (m.Strength != null && strength != null && m.Strength.ToLower() == strength.ToLower())) &&
                    ((m.Unit == null && unit == null) || (m.Unit != null && unit != null && m.Unit.ToLower() == unit.ToLower())) &&
                    ((m.Category == null && category == null) || (m.Category != null && category != null && m.Category.ToLower() == category.ToLower())) &&
                    ((m.HSNCode == null && hsnCode == null) || (m.HSNCode != null && hsnCode != null && m.HSNCode.ToLower() == hsnCode.ToLower())) &&
                    ((m.RackLocation == null && rack == null) || (m.RackLocation != null && rack != null && m.RackLocation.ToLower() == rack.ToLower())));

            if (existing != null)
            {
                result.Skipped++;
            }
            else
            {
                context.MedicineMasters.Add(new MedicineMaster
                {
                    GenericName = genericName,
                    BrandName = brandName,
                    Manufacturer = manufacturer,
                    Form = form,
                    Strength = strength,
                    Unit = unit,
                    Category = category,
                    HSNCode = hsnCode,
                    RackLocation = rack,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                result.Added++;
            }

            row++;
        }

        await context.SaveChangesAsync();
        result.TotalProcessed = result.Added + result.Updated + result.Skipped;
        return result;
    }

    private static (string? form, string? strength) ParseStrengthForm(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        raw = raw.Trim();
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[^1].Length <= 15)
        {
            var strength = string.Join(" ", parts[..^1]);
            var form = parts[^1];
            return (form, strength);
        }

        return (raw, null);
    }

    public byte[] GenerateTemplate()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Medicine Master Template");
        sheet.Cells[1, 1].Value = "Generic Name *";
        sheet.Cells[1, 2].Value = "Brand Name";
        sheet.Cells[1, 3].Value = "Manufacturer";
        sheet.Cells[1, 4].Value = "Form";
        sheet.Cells[1, 5].Value = "Strength";
        sheet.Cells[1, 6].Value = "Unit";
        sheet.Cells[1, 7].Value = "Category";

        using var range = sheet.Cells[1, 1, 1, 7];
        range.Style.Font.Bold = true;
        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

        sheet.Cells[2, 1].Value = "Paracetamol";
        sheet.Cells[2, 2].Value = "Calpol";
        sheet.Cells[2, 3].Value = "GlaxoSmithKline";
        sheet.Cells[2, 4].Value = "Tablet";
        sheet.Cells[2, 5].Value = "500mg";
        sheet.Cells[2, 6].Value = "Strip";
        sheet.Cells[2, 7].Value = "Analgesic";

        for (var c = 1; c <= 7; c++) sheet.Column(c).AutoFit();
        return package.GetAsByteArray();
    }

    public byte[] ExportToExcel(List<MedicineMaster> list)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Medicine Master");
        sheet.Cells[1, 1].Value = "Generic Name";
        sheet.Cells[1, 2].Value = "Brand Name";
        sheet.Cells[1, 3].Value = "Manufacturer";
        sheet.Cells[1, 4].Value = "Form";
        sheet.Cells[1, 5].Value = "Strength";
        sheet.Cells[1, 6].Value = "Unit";
        sheet.Cells[1, 7].Value = "Category";

        using var range = sheet.Cells[1, 1, 1, 7];
        range.Style.Font.Bold = true;

        var row = 2;
        foreach (var m in list)
        {
            sheet.Cells[row, 1].Value = m.GenericName;
            sheet.Cells[row, 2].Value = m.BrandName;
            sheet.Cells[row, 3].Value = m.Manufacturer;
            sheet.Cells[row, 4].Value = m.Form;
            sheet.Cells[row, 5].Value = m.Strength;
            sheet.Cells[row, 6].Value = m.Unit;
            sheet.Cells[row, 7].Value = m.Category;
            row++;
        }

        for (var c = 1; c <= 7; c++) sheet.Column(c).AutoFit();
        return package.GetAsByteArray();
    }

    public byte[] ExportToCsv(List<MedicineMaster> list)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        writer.WriteLine("Generic Name,Brand Name,Manufacturer,Form,Strength,Unit,Category");
        foreach (var m in list)
        {
            writer.WriteLine($"{EscapeCsv(m.GenericName)},{EscapeCsv(m.BrandName)},{EscapeCsv(m.Manufacturer)},{EscapeCsv(m.Form)},{EscapeCsv(m.Strength)},{EscapeCsv(m.Unit)},{EscapeCsv(m.Category)}");
        }
        writer.Flush();
        return ms.ToArray();
    }

    private static string EscapeCsv(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : $"\"{value.Replace("\"", "\"\"")}\"";
}

public class ImportResult
{
    public int TotalProcessed { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}
