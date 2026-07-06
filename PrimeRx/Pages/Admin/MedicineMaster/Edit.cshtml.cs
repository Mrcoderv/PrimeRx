using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.MedicineMaster;

public class EditModel(MedicineMasterService service) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var master = await service.GetByIdAsync(id);
        if (master == null) return NotFound();

        Input = new InputModel
        {
            Id = master.Id,
            GenericName = master.GenericName,
            BrandName = master.BrandName,
            Manufacturer = master.Manufacturer,
            Form = master.Form,
            Strength = master.Strength,
            Unit = master.Unit,
            Category = master.Category,
            HSNCode = master.HSNCode,
            RackLocation = master.RackLocation
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var master = await service.GetByIdAsync(Input.Id);
        if (master == null) return NotFound();

        master.GenericName = Input.GenericName;
        master.BrandName = Input.BrandName;
        master.Manufacturer = Input.Manufacturer;
        master.Form = Input.Form;
        master.Strength = Input.Strength;
        master.Unit = Input.Unit;
        master.Category = Input.Category;
        master.HSNCode = Input.HSNCode;
        master.RackLocation = Input.RackLocation;

        await service.UpdateAsync(master);
        return RedirectToPage("Index", new { message = $"Medicine '{master.DisplayName}' updated." });
    }

    public class InputModel
    {
        public int Id { get; set; }
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
