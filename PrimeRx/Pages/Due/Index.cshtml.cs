using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Due;

public class IndexModel(DueService dueService) : PageModel
{
    public List<Bill> DueBills { get; set; } = [];
    public string? Search { get; set; }

    // Ageing buckets
    public DueAgeingBucket Age_0_30 { get; set; } = new();
    public DueAgeingBucket Age_31_60 { get; set; } = new();
    public DueAgeingBucket Age_61_90 { get; set; } = new();
    public DueAgeingBucket Age_90_Plus { get; set; } = new();
    public decimal TotalOutstanding { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;
        DueBills = await dueService.GetDueBillsAsync(search);

        TotalOutstanding = DueBills.Sum(b => b.DueAmount);

        var today = DateTime.Today;
        Age_0_30 = new DueAgeingBucket { Label = "0–30 Days" };
        Age_31_60 = new DueAgeingBucket { Label = "31–60 Days" };
        Age_61_90 = new DueAgeingBucket { Label = "61–90 Days" };
        Age_90_Plus = new DueAgeingBucket { Label = "90+ Days" };

        foreach (var bill in DueBills)
        {
            var daysSinceBill = (today - bill.BillDate.Date).Days;
            var bucket = daysSinceBill switch
            {
                <= 30 => Age_0_30,
                <= 60 => Age_31_60,
                <= 90 => Age_61_90,
                _ => Age_90_Plus
            };
            bucket.Add(bill);
        }
    }

    public class DueAgeingBucket
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public void Add(Bill b) { Count++; Amount += b.DueAmount; }
    }
}
