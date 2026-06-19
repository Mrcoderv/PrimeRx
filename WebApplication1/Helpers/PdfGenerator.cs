using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication1.Models;

namespace WebApplication1.Helpers;

public class PdfGenerator
{
    public PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoice(Bill bill, CompanyProfile company)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(company.Name).Bold().FontSize(16);
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        col.Item().Text(company.Address);
                    col.Item().Text($"Phone: {company.Phone}");
                    if (!string.IsNullOrWhiteSpace(company.GSTIN))
                        col.Item().Text($"GSTIN: {company.GSTIN}");
                    col.Item().PaddingTop(5).LineHorizontal(1);
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
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Medicine").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Rate").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Qty").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Disc").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Amount").Bold();
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
                        c.Item().Text($"Total: {bill.FinalAmount:N2}").Bold().FontSize(12);
                        c.Item().Text($"Payment: {bill.PaymentMethod}");
                        c.Item().Text($"Status: {bill.PaymentStatus}");
                        if (bill.DueAmount > 0)
                            c.Item().Text($"Due: {bill.DueAmount:N2}").FontColor(Colors.Red.Medium);
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for your purchase!");
            });
        }).GeneratePdf();
    }
}
