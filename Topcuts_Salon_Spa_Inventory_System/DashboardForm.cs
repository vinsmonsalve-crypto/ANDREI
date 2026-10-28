using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;

namespace CIT_Inventory_Management_System;

public class DashboardForm : Form
{
    private readonly Label productsValue = new();
    private readonly Label lowStockValue = new();
    private readonly Label salesValue = new();
    private readonly Label inventoryValue = new();
    private readonly Label profitValue = new();
    private readonly Label alertBanner = new();
    private readonly DataGridView lowStockGrid = new();
    private Panel lowStockCard = null!;
    private bool alertShown;

    public DashboardForm()
    {
        Text = "Topcuts Salon & Spa Inventory System - Dashboard";
        ClientSize = new Size(1180, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 252);

        var side = new Panel { Dock = DockStyle.Left, Width = 205, BackColor = Ui.Green };
        var logo = Ui.Logo(125); logo.Location = new Point(40, 22); side.Controls.Add(logo);
        var brand = new Label { Text = "TOPCUTS", ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, Location = new Point(48, 165) }; side.Controls.Add(brand);
        var sub = new Label { Text = "SALON & SPA", ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(55, 194) }; side.Controls.Add(sub);

        var inventory = SideButton("INVENTORY", 235); inventory.Click += (_, _) => { using var f = new ProductForm(); f.ShowDialog(this); RefreshDashboard(); }; side.Controls.Add(inventory);
        var sales = SideButton("SALES", 285); sales.Click += (_, _) => { using var f = new SalesForm(); f.ShowDialog(this); RefreshDashboard(); }; side.Controls.Add(sales);
        var restock = SideButton("RESTOCK", 335); restock.Click += (_, _) => { using var f = new RestockForm(); f.ShowDialog(this); RefreshDashboard(); }; side.Controls.Add(restock);
        var history = SideButton("HISTORY", 385); history.Click += (_, _) => { using var f = new MovementHistoryForm(); f.ShowDialog(this); }; side.Controls.Add(history);
        var suppliers = SideButton("SUPPLIERS", 435); suppliers.Click += (_, _) => { using var f = new SupplierForm(); f.ShowDialog(this); }; side.Controls.Add(suppliers);
        var orders = SideButton("PURCHASE ORDERS", 485); orders.Click += (_, _) => { OpenPurchaseOrders(); }; side.Controls.Add(orders);
        var logout = SideButton("LOG OUT", 595); logout.BackColor = Color.FromArgb(190, 55, 45); logout.Click += (_, _) => Close(); side.Controls.Add(logout);
        Controls.Add(side);

        var headerLogo = Ui.Logo(60); headerLogo.Location = new Point(230, 22); Controls.Add(headerLogo);
        var title = Ui.Title("TOPCUTS INVENTORY DASHBOARD", 18); title.Location = new Point(300, 35); Controls.Add(title);
        var user = new Label { Text = $"Signed in as: {Session.FullName} (@{Session.Username})", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(305, 70) }; Controls.Add(user);

        
        void CenterHeader()
        {
            var centerX = side.Width + (ClientSize.Width - side.Width) / 2;
            title.Location = new Point(centerX - title.Width / 2, title.Top);
            user.Location = new Point(centerX - user.Width / 2, user.Top);
        }
        Load += (_, _) => CenterHeader();
        Resize += (_, _) => CenterHeader();

        AddStat("PRODUCTS", productsValue, 230, 115, out _);
        AddStat("LOW STOCK", lowStockValue, 410, 115, out lowStockCard);
        AddStat("TODAY'S SALES", salesValue, 590, 115, out _);
        AddStat("TODAY'S PROFIT", profitValue, 770, 115, out _);
        AddStat("INVENTORY UNITS", inventoryValue, 950, 115, out _);

        var info = new Panel { Location = new Point(230, 240), Size = new Size(900, 420), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var infoTitle = Ui.Title("Low Stock Alerts", 17); infoTitle.Location = new Point(25, 15); info.Controls.Add(infoTitle);
        alertBanner.Location = new Point(28, 58); alertBanner.Size = new Size(840, 28); alertBanner.TextAlign = ContentAlignment.MiddleLeft; alertBanner.Padding = new Padding(10, 0, 0, 0); alertBanner.Font = new Font("Segoe UI", 10, FontStyle.Bold); info.Controls.Add(alertBanner);
        lowStockGrid.Location = new Point(28, 95); lowStockGrid.Size = new Size(840, 250); lowStockGrid.ReadOnly = true; lowStockGrid.AllowUserToAddRows = false; lowStockGrid.AllowUserToDeleteRows = false; lowStockGrid.RowHeadersVisible = false; lowStockGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; lowStockGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; lowStockGrid.BackgroundColor = Color.White;
        info.Controls.Add(lowStockGrid);
        var reorder = Ui.Button("CREATE PURCHASE ORDER", 230); reorder.Location = new Point(28, 360); reorder.Click += (_, _) => OpenPurchaseOrders(); info.Controls.Add(reorder);
        var restockNow = Ui.Button("RESTOCK NOW", 160); restockNow.BackColor = Color.FromArgb(29, 64, 125); restockNow.Location = new Point(270, 360); restockNow.Click += (_, _) => { using var f = new RestockForm(); f.ShowDialog(this); RefreshDashboard(); }; info.Controls.Add(restockNow);
        Controls.Add(info);
        Shown += (_, _) => RefreshDashboard();
    }

    private Button SideButton(string text, int y)
    {
        var b = Ui.Button(text, 165); b.Location = new Point(20, y); b.BackColor = Color.FromArgb(29, 64, 125); return b;
    }

    private void OpenPurchaseOrders()
    {
        using var f = new PurchaseOrderForm(); f.ShowDialog(this); RefreshDashboard();
    }

    private void AddStat(string name, Label value, int x, int y, out Panel card)
    {
        var panel = new Panel { Location = new Point(x, y), Size = new Size(170, 105), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        card = panel;
        panel.Controls.Add(new Label { Text = name, Location = new Point(15, 13), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        value.Location = new Point(15, 42); value.AutoSize = true; value.Font = new Font("Segoe UI", 17, FontStyle.Bold); value.ForeColor = Ui.Green; panel.Controls.Add(value);
        Controls.Add(panel);
    }

    private void RefreshDashboard()
    {
        try
        {
            using var connection = DatabaseConnection.GetConnection(); connection.Open();
            productsValue.Text = Convert.ToString(new MySqlCommand("SELECT COUNT(*) FROM products WHERE is_active=1", connection).ExecuteScalar())!;
            salesValue.Text = "₱" + Convert.ToDecimal(new MySqlCommand("SELECT COALESCE(SUM(total),0) FROM sales WHERE DATE(created_at)=CURDATE()", connection).ExecuteScalar()).ToString("N2");
            profitValue.Text = "₱" + Convert.ToDecimal(new MySqlCommand(@"
                SELECT COALESCE(SUM((si.unit_price - si.unit_cost) * si.quantity), 0)
                FROM sale_items si INNER JOIN sales s ON s.id = si.sale_id
                WHERE DATE(s.created_at)=CURDATE()", connection).ExecuteScalar()).ToString("N2");
            inventoryValue.Text = Convert.ToString(new MySqlCommand("SELECT COALESCE(SUM(quantity),0) FROM products WHERE is_active=1", connection).ExecuteScalar())!;
            LoadLowStock(connection);
        }
        catch { productsValue.Text = lowStockValue.Text = salesValue.Text = profitValue.Text = inventoryValue.Text = "—"; }
    }

    private void LoadLowStock(MySqlConnection connection)
    {
        using var cmd = new MySqlCommand(@"
            SELECT product_id AS 'Product ID', product_name AS Product, category AS Category,
                   quantity AS 'In Stock', low_stock_threshold AS 'Alert Level',
                   CASE WHEN quantity = 0 THEN 'OUT OF STOCK' ELSE 'LOW STOCK' END AS Status
            FROM products
            WHERE is_active=1 AND quantity <= low_stock_threshold
            ORDER BY quantity ASC, product_name;", connection);
        using var adapter = new MySqlDataAdapter(cmd); var table = new System.Data.DataTable(); adapter.Fill(table);
        lowStockGrid.DataSource = table;

        var count = table.Rows.Count;
        lowStockValue.Text = count.ToString();
        lowStockValue.ForeColor = count > 0 ? Color.FromArgb(190, 55, 45) : Ui.Green;
        lowStockCard.BackColor = count > 0 ? Color.FromArgb(255, 240, 238) : Color.White;
        if (count > 0)
        {
            var outCount = table.Rows.Cast<System.Data.DataRow>().Count(r => Convert.ToInt32(r["In Stock"]) == 0);
            alertBanner.Text = $"⚠ {count} product(s) need restocking" + (outCount > 0 ? $" — {outCount} out of stock." : ".");
            alertBanner.BackColor = Color.FromArgb(255, 226, 222); alertBanner.ForeColor = Color.FromArgb(150, 35, 25);
        }
        else
        {
            alertBanner.Text = "✔ All products are above their low-stock level.";
            alertBanner.BackColor = Color.FromArgb(226, 245, 230); alertBanner.ForeColor = Color.FromArgb(25, 110, 50);
        }


        if (count > 0 && !alertShown)
        {
            alertShown = true;
            var top = table.Rows.Cast<System.Data.DataRow>().Take(8).Select(r => $"• {r["Product"]} — {r["In Stock"]} left (alert at {r["Alert Level"]})");
            var more = count > 8 ? $"\n…and {count - 8} more." : "";
            MessageBox.Show("Low stock alert!\n\n" + string.Join("\n", top) + more, "Low Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
