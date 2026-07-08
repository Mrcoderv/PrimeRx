using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.MedicineMaster;

public class IndexModel(MedicineMasterService service) : PageModel
{
    public List<Models.MedicineMaster> Masters { get; set; } = [];
    public HashSet<string> GenericNamesInStock { get; set; } = [];
    public string? Search { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public bool ShowImportModal { get; set; }

    public async Task OnGetAsync(string? search, string? message, bool isError = false)
    {
        Search = search;
        Message = message;
        IsError = isError;
        Masters = await service.GetAllAsync(search);
        GenericNamesInStock = await service.GetGenericNamesWithStockAsync();
    }

    public async Task<IActionResult> OnGetTemplateAsync()
    {
        var bytes = service.GenerateTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "medicine-master-template.xlsx");
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var list = await service.GetAllAsync();
        var bytes = service.ExportToExcel(list);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "medicine-master.xlsx");
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var list = await service.GetAllAsync();
        var bytes = service.ExportToCsv(list);
        return File(bytes, "text/csv", "medicine-master.csv");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile importFile, bool updateExisting = true)
    {
        if (importFile == null || importFile.Length == 0)
        {
            return RedirectToPage(new { message = "Please select a file to import.", isError = true, showImportModal = true });
        }

        using var stream = new MemoryStream();
        await importFile.CopyToAsync(stream);
        stream.Position = 0;

        var result = await service.ImportFromExcelAsync(stream, updateExisting);

        var msg = result.Errors.Count > 0
            ? $"Import completed with errors: {string.Join("; ", result.Errors)}"
            : $"Import complete: {result.Added} added, {result.Updated} updated, {result.Skipped} skipped.";

        return RedirectToPage(new { message = msg, isError = result.Errors.Count > 0 });
    }
}
