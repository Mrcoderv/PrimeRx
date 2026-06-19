using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PrimeRx.Models;

namespace PrimeRx.Helpers;

public class PdfGenerator
{
    public PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoice(Bill bill, CompanyProfile company)
    {
        var headerColor = ParseColor(company.BillPrimaryColor);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(company.BillTitle).Bold().FontSize(14).FontColor(headerColor);
                    col.Item().Text(company.Name).Bold().FontSize(16);
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        col.Item().Text(company.Address);
                    if (!string.IsNullOrWhiteSpace(company.Phone))
                        col.Item().Text($"Phone: {company.Phone}");
                    if (company.ShowPanOnBill && !string.IsNullOrWhiteSpace(company.PAN))
                        col.Item().Text($"PAN: {company.PAN}");
                    if (company.ShowGstinOnBill && !string.IsNullOrWhiteSpace(company.GSTIN))
                        col.Item().Text($"GSTIN: {company.GSTIN}");
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(headerColor);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Bill #: {bill.BillNumber}").Bold();
                            c.Item().Text($"Date: {bill.BillDate:dd-MM-yyyy HH:mm}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Customer: {bill.CustomerName}");
                            c.Item().Text($"Phone: {bill.CustomerPhone}");
                        });
                    });

                    col.Item().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(headerColor).Padding(4).Text("Medicine").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(4).Text("Rate").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(4).Text("Qty").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(4).Text("Disc").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(4).Text("Amount").Bold().FontColor(Colors.White);
                        });

                        foreach (var item in bill.SaleItems)
                        {
                            table.Cell().Padding(4).Text(item.MedicineName);
                            table.Cell().Padding(4).Text(item.Rate.ToString("N2"));
                            table.Cell().Padding(4).Text(item.Quantity.ToString());
                            table.Cell().Padding(4).Text(item.DiscountPerItem.ToString("N2"));
                            table.Cell().Padding(4).Text(item.Amount.ToString("N2"));
                        }
                    });

                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal: {bill.TotalAmount:N2}");
                        if (bill.DiscountAmount > 0)
                            c.Item().Text($"Discount: -{bill.DiscountAmount:N2}");
                        if (bill.TaxAmount > 0)
                            c.Item().Text($"{company.TaxLabel}: {bill.TaxAmount:N2}");
                        c.Item().Text($"Total: {bill.FinalAmount:N2}").Bold().FontSize(12);
                        c.Item().Text($"Payment: {bill.PaymentMethod}");
                        c.Item().Text($"Status: {bill.PaymentStatus}");
                        if (bill.DueAmount > 0)
                            c.Item().Text($"Due: {bill.DueAmount:N2}").FontColor(Colors.Red.Medium);
                    });
                });

                var footer = string.IsNullOrWhiteSpace(company.BillFooterText)
                    ? "Thank you for your purchase!"
                    : company.BillFooterText;
                page.Footer().AlignCenter().Text(footer).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    private static string ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith('#') || hex.Length < 7)
            return "#2563eb";
        return hex;
    }
}
