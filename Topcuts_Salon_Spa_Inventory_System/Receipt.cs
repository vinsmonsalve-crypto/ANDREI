using System.Drawing.Printing;

namespace CIT_Inventory_Management_System;

/// <summary>One line on a receipt.</summary>
public sealed record ReceiptLine(string Name, int Quantity, decimal UnitPrice)
{
    public decimal Total => UnitPrice * Quantity;
}

/// <summary>Everything needed to print a sales receipt.</summary>
public sealed record ReceiptData(string InvoiceNo, DateTime Date, string Cashier, IReadOnlyList<ReceiptLine> Lines)
{
    public decimal Total => Lines.Sum(l => l.Total);
}

/// <summary>Shows a print preview (with a Print button) for a sales receipt on a narrow, thermal-style page.</summary>
public static class ReceiptPrinter
{
    private const int PageWidth = 315; // hundredths of an inch (~80 mm)

    public static void ShowPreview(IWin32Window? owner, ReceiptData receipt)
    {
        using var document = new PrintDocument { DocumentName = receipt.InvoiceNo };
        var height = 360 + receipt.Lines.Count * 42;
        document.DefaultPageSettings.PaperSize = new PaperSize("Receipt", PageWidth, Math.Max(height, 500));
        document.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
        document.PrintPage += (_, e) => Draw(e, receipt);

        using var preview = new PrintPreviewDialog { Document = document, Width = 520, Height = 760, StartPosition = FormStartPosition.CenterParent };
        preview.Text = "Receipt - " + receipt.InvoiceNo;
        if (owner != null) preview.ShowDialog(owner); else preview.ShowDialog();
    }

    private static void Draw(PrintPageEventArgs e, ReceiptData r)
    {
        var g = e.Graphics!;
        using var title = new Font("Segoe UI", 13, FontStyle.Bold);
        using var bold = new Font("Segoe UI", 9, FontStyle.Bold);
        using var normal = new Font("Segoe UI", 9);
        using var small = new Font("Segoe UI", 8);
        using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        var left = e.MarginBounds.Left;
        var width = e.MarginBounds.Width;
        float y = e.MarginBounds.Top;
        var center = new StringFormat { Alignment = StringAlignment.Center };
        var right = new StringFormat { Alignment = StringAlignment.Far };
        var clip = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        void Centered(string text, Font font)
        {
            g.DrawString(text, font, Brushes.Black, new RectangleF(left, y, width, font.Height + 2), center);
            y += font.Height + 2;
        }
        void Rule() { y += 4; g.DrawLine(pen, left, y, left + width, y); y += 6; }

        Centered("TOPCUTS SALON & SPA", title);
        Centered("SALES RECEIPT", bold);
        Rule();
        g.DrawString("Invoice: " + r.InvoiceNo, small, Brushes.Black, new RectangleF(left, y, width, small.Height + 2), clip); y += small.Height + 2;
        g.DrawString("Date: " + r.Date.ToString("yyyy-MM-dd HH:mm"), small, Brushes.Black, left, y); y += small.Height + 2;
        g.DrawString("Cashier: " + r.Cashier, small, Brushes.Black, new RectangleF(left, y, width, small.Height + 2), clip); y += small.Height + 2;
        Rule();

        foreach (var line in r.Lines)
        {
            g.DrawString(line.Name, bold, Brushes.Black, new RectangleF(left, y, width, bold.Height + 2), clip); y += bold.Height + 2;
            g.DrawString($"{line.Quantity} x ₱{line.UnitPrice:N2}", normal, Brushes.Black, left + 8, y);
            g.DrawString($"₱{line.Total:N2}", normal, Brushes.Black, new RectangleF(left, y, width, normal.Height + 2), right);
            y += normal.Height + 6;
        }

        Rule();
        g.DrawString("TOTAL", title, Brushes.Black, left, y);
        g.DrawString($"₱{r.Total:N2}", title, Brushes.Black, new RectangleF(left, y, width, title.Height + 2), right);
        y += title.Height + 10;
        Centered("Thank you for visiting Topcuts!", small);
        Centered("Please come again.", small);

        e.HasMorePages = false;
    }
}
