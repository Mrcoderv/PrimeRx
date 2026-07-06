using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.MedicineMaster;

public class CreateModel(MedicineMasterService service) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var master = new Models.MedicineMaster
        {
            GenericName = Input.GenericName,
            BrandName = Input.BrandName,
            Manufacturer = Input.Manufacturer,
            Form = Input.Form,
            Strength = Input.Strength,
            Unit = Input.Unit,
            Category = Input.Category,
            HSNCode = Input.HSNCode,
            RackLocation = Input.RackLocation
        };

        await service.CreateAsync(master);
        return RedirectToPage("Index", new { message = $"Medicine '{master.DisplayName}' added to master list." });
    }

    public class InputModel
    {
        public string GenericName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? Manufacturer { get; set; }
        public string? Form { get; set; }
        public string? Strength { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public string? HSNCode { get; set; }
        public string? RackLocation { get; set; }
    }
}
