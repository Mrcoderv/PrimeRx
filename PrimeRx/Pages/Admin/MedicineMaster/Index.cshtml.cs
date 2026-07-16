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
    public string? Letter { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public bool ShowImportModal { get; set; }
    public int TotalCount { get; set; }

    public async Task OnGetAsync(string? search, string? letter, string? message, bool isError = false)
    {
        Search = search;
        Letter = letter;
        Message = message;
        IsError = isError;
        Masters = await service.GetAllAsync(search, letter);
        GenericNamesInStock = await service.GetGenericNamesWithStockAsync();
        TotalCount = await service.GetCountAsync();
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
