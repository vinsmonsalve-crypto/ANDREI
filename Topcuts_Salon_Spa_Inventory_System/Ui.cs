using System.Drawing.Drawing2D;

namespace CIT_Inventory_Management_System;

public static class Ui
{
    public static Color Green => Color.FromArgb(20, 48, 105);
    public static Color LightGreen => Color.FromArgb(235, 241, 252);
    public static Color Gold => Color.FromArgb(245, 199, 24);
    public static Color Dark => Color.FromArgb(30, 30, 30);

    public static Button Button(string text, int width = 120)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            BackColor = Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
    }

    public static PictureBox Logo(int size = 110)
    {
        var picture = new PictureBox
        {
            Width = size,
            Height = size,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "topcuts.jpg");
        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            picture.Image = Image.FromStream(stream).Clone() as Image;
        }
        return picture;
    }

    public static Label Title(string text, int size = 20)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", size, FontStyle.Bold),
            ForeColor = Green
        };
    }

    public static void RoundPanel(Panel panel)
    {
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(215, 225, 212));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
    }
}
