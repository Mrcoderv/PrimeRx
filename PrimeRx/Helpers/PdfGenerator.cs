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
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                // ==================== HEADER ====================
                page.Header().Column(col =>
                {
                    col.Item().Text(company.Name).Bold().FontSize(14).AlignCenter();
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        col.Item().Text(company.Address).AlignCenter().FontSize(9);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text($"Ph: {company.Phone}");
                        if (company.ShowPanOnBill && !string.IsNullOrWhiteSpace(company.PAN))
                            row.RelativeItem().AlignCenter().Text($"PAN: {company.PAN}");
                        if (company.ShowGstinOnBill && !string.IsNullOrWhiteSpace(company.GSTIN))
                            row.RelativeItem().AlignCenter().Text($"GSTIN: {company.GSTIN}");
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(headerColor);

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Invoice No: {bill.BillNumber}").Bold();
                        row.RelativeItem().AlignRight().Text($"Date: {bill.BillDate:dd/MM/yyyy HH:mm}");
                    });
                });

                // ==================== CONTENT ====================
                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Customer Info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Customer: {bill.CustomerName ?? "Cash Customer"}");
                        if (!string.IsNullOrWhiteSpace(bill.CustomerPhone))
                            row.RelativeItem().AlignRight().Text($"Phone: {bill.CustomerPhone}");
                    });

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);   // S.No
                            c.RelativeColumn(3.5);  // Item Description
                            c.RelativeColumn(1);    // Pack
                            c.RelativeColumn(1.2);  // Batch
                            c.RelativeColumn(1);    // Exp
                            c.RelativeColumn(0.8);  // Qty
                            c.RelativeColumn(1);    // Rate
                            c.RelativeColumn(1);    // MRP
                            c.RelativeColumn(1);    // Disc
                            c.RelativeColumn(1.2);  // Amount
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(headerColor).Padding(5).Text("S.No").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Item Description").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Pack").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Batch").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Exp").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Qty").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Rate").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("MRP").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Disc").Bold().FontColor(Colors.White);
                            h.Cell().Background(headerColor).Padding(5).Text("Amount").Bold().FontColor(Colors.White);
                        });

                        int index = 1;
                        foreach (var item in bill.SaleItems)
                        {
                            table.Cell().Padding(4).Text(index++.ToString());
                            table.Cell().Padding(4).Text(item.MedicineName);
                            table.Cell().Padding(4).Text(item.PackSize ?? "-");
                            table.Cell().Padding(4).Text(item.BatchNumber ?? "-");
                            table.Cell().Padding(4).Text(item.ExpiryDate?.ToString("MM/yy") ?? "-");
                            table.Cell().Padding(4).Text(item.Quantity.ToString());
                            table.Cell().Padding(4).Text(item.Rate.ToString("N2"));
                            table.Cell().Padding(4).Text(item.MRP.ToString("N2"));
                            table.Cell().Padding(4).Text(item.DiscountPerItem > 0 ? $"-{item.DiscountPerItem:N2}" : "0.00");
                            table.Cell().Padding(4).Text(item.Amount.ToString("N2"));
                        }
                    });

                    // ==================== TOTALS ====================
                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal          : {bill.TotalAmount:N2}");
                        if (bill.DiscountAmount > 0)
                            c.Item().Text($"Discount          : -{bill.DiscountAmount:N2}");
                        if (bill.TaxAmount > 0)
                            c.Item().Text($"{company.TaxLabel ?? "Tax"}     : {bill.TaxAmount:N2}");

                        c.Item().Text($"**Net Total       : {bill.FinalAmount:N2}**").Bold().FontSize(11);

                        if (bill.DueAmount > 0)
                            c.Item().Text($"Due Amount        : {bill.DueAmount:N2}").FontColor(Colors.Red.Medium);

                        c.Item().PaddingTop(5).Text($"Payment Method : {bill.PaymentMethod}");
                    });
                });

                // ==================== FOOTER ====================
                page.Footer().Column(footerCol =>
                {
                    // Amount in Words
                    var amountInWords = NumberToWordsConverter.ToWords((long)bill.FinalAmount) + " Only";
                    footerCol.Item().Text($"Rupees : {amountInWords}").Bold().FontSize(10);

                    // Your requested lines
                    footerCol.Item().PaddingTop(8).Text("Wishing your good health ::").FontSize(10).AlignCenter();

                    var footerText = string.IsNullOrWhiteSpace(company.BillFooterText)
                        ? "Powered by Prime LogicTech"
                        : company.BillFooterText;

                    footerCol.Item().PaddingTop(5).Text(footerText).FontSize(9).AlignCenter().FontColor(Colors.Grey.Darken1);
                });
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
