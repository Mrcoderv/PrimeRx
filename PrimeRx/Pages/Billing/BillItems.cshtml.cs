using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrimeRx.Pages.Billing;

public class BillItemsModel : PageModel
{
    public List<BillItem> Items { get; set; } = [];
    public decimal BillDiscountPercent { get; set; }
    public decimal BillDiscountAmount { get; set; }

    public void OnGet()
    {
        BillDiscountPercent = 8;
        BillDiscountAmount = 7.60m;

        Items =
        [
            new()
            {
                MedicineName = "Betadine Solution 100ml",
                Stock = 90,
                Rate = 95.00m,
                Qty = 1,
                DiscPercent = 0.00m,
                DiscAmount = 0.00m,
                Amount = 95.00m
            }
        ];
    }

    public class BillItem
    {
        public string MedicineName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal Rate { get; set; }
        public int Qty { get; set; }
        public decimal DiscPercent { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal Amount { get; set; }
    }
}
