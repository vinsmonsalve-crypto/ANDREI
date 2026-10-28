using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;
using System.Data;

namespace CIT_Inventory_Management_System;

public class PurchaseOrderForm : Form
{
    // New order tab
    private readonly ComboBox supplier = new();
    private readonly ComboBox product = new();
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 100000, Value = 1 };
    private readonly NumericUpDown unitCost = new() { DecimalPlaces = 2, Minimum = 0, Maximum = 1000000 };
    private readonly DataGridView lineGrid = new();
    private readonly Label orderTotal = new();
    private readonly TextBox notes = new();
    private readonly List<OrderLine> lines = new();

    // Orders tab
    private readonly DataGridView orderGrid = new();
    private readonly DataGridView itemGrid = new();

    public PurchaseOrderForm()
    {
        Text = "Purchase Orders"; ClientSize = new Size(1100, 700); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(245, 247, 252);
        var title = Ui.Title("PURCHASE ORDERS", 21); title.Location = new Point(30, 15); Controls.Add(title);
        var logo = Ui.Logo(60); logo.Location = new Point(1010, 5); Controls.Add(logo);

        var tabs = new TabControl { Location = new Point(20, 70), Size = new Size(1060, 610) };
        var newTab = new TabPage("New Order") { BackColor = Color.White };
        var ordersTab = new TabPage("Orders") { BackColor = Color.White };
        tabs.TabPages.Add(newTab); tabs.TabPages.Add(ordersTab);
        Controls.Add(tabs);
        tabs.SelectedIndexChanged += (_, _) => { if (tabs.SelectedTab == ordersTab) LoadOrders(); };

        BuildNewOrderTab(newTab);
        BuildOrdersTab(ordersTab);
        Load += (_, _) => LoadLookups();
    }

    // ---------- New order tab ----------

    private void BuildNewOrderTab(TabPage tab)
    {
        void AddLabel(string text, int x, int y) => tab.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) });

        AddLabel("Supplier", 20, 20); supplier.Location = new Point(20, 43); supplier.Width = 300; supplier.DropDownStyle = ComboBoxStyle.DropDownList; tab.Controls.Add(supplier);
        AddLabel("Product", 340, 20); product.Location = new Point(340, 43); product.Width = 340; product.DropDownStyle = ComboBoxStyle.DropDownList; product.SelectedIndexChanged += (_, _) => UseProductCost(); tab.Controls.Add(product);
        AddLabel("Quantity", 700, 20); quantity.Location = new Point(700, 43); quantity.Width = 80; tab.Controls.Add(quantity);
        AddLabel("Unit Cost", 800, 20); unitCost.Location = new Point(800, 43); unitCost.Width = 110; tab.Controls.Add(unitCost);
        var add = Ui.Button("ADD LINE", 110); add.Location = new Point(925, 38); add.Click += AddLine_Click; tab.Controls.Add(add);

        lineGrid.Location = new Point(20, 90); lineGrid.Size = new Size(1015, 290); lineGrid.ReadOnly = true; lineGrid.AllowUserToAddRows = false; lineGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; lineGrid.MultiSelect = false; lineGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        lineGrid.Columns.Add("code", "Product ID"); lineGrid.Columns.Add("name", "Product"); lineGrid.Columns.Add("qty", "Quantity"); lineGrid.Columns.Add("cost", "Unit Cost"); lineGrid.Columns.Add("total", "Line Total");
        lineGrid.Columns[3].DefaultCellStyle.Format = "₱#,##0.00"; lineGrid.Columns[4].DefaultCellStyle.Format = "₱#,##0.00";
        tab.Controls.Add(lineGrid);

        var remove = Ui.Button("REMOVE LINE", 140); remove.BackColor = Color.FromArgb(190, 55, 45); remove.Location = new Point(20, 392); remove.Click += RemoveLine_Click; tab.Controls.Add(remove);
        orderTotal.Text = "ORDER TOTAL: ₱0.00"; orderTotal.AutoSize = true; orderTotal.Location = new Point(700, 395); orderTotal.Font = new Font("Segoe UI", 14, FontStyle.Bold); orderTotal.ForeColor = Ui.Green; tab.Controls.Add(orderTotal);

        AddLabel("Notes (optional)", 20, 445); notes.Location = new Point(20, 468); notes.Width = 700; tab.Controls.Add(notes);
        var save = Ui.Button("SAVE ORDER", 180); save.Location = new Point(740, 460); save.Click += SaveOrder_Click; tab.Controls.Add(save);
        var hint = new Label { Text = "A saved order is PENDING. Open the Orders tab and press RECEIVE when the delivery arrives to add the stock.", Location = new Point(20, 520), AutoSize = true, ForeColor = Color.DimGray };
        tab.Controls.Add(hint);
    }

    private void LoadLookups()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            supplier.Items.Clear(); product.Items.Clear();
            using (var cmd = new MySqlCommand("SELECT id, supplier_name FROM suppliers WHERE is_active=1 ORDER BY supplier_name", c))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) supplier.Items.Add(new Choice(r.GetInt32("id"), r.GetString("supplier_name"), 0m));
            using (var cmd = new MySqlCommand("SELECT id, product_id, product_name, cost_price FROM products WHERE is_active=1 ORDER BY product_name", c))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) product.Items.Add(new Choice(r.GetInt32("id"), r.GetString("product_id") + " - " + r.GetString("product_name"), r.GetDecimal("cost_price"), r.GetString("product_id"), r.GetString("product_name")));
            if (supplier.Items.Count > 0) supplier.SelectedIndex = 0;
            if (product.Items.Count > 0) product.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show("Could not load suppliers and products.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void UseProductCost()
    {
        if (product.SelectedItem is Choice p) unitCost.Value = Math.Min(p.Cost, unitCost.Maximum);
    }

    private void AddLine_Click(object? sender, EventArgs e)
    {
        if (product.SelectedItem is not Choice p) { MessageBox.Show("Select a product."); return; }
        var existing = lines.FirstOrDefault(l => l.ProductId == p.Id);
        if (existing != null) { existing.Quantity += (int)quantity.Value; existing.UnitCost = unitCost.Value; }
        else lines.Add(new OrderLine(p.Id, p.Code, p.ProductName, (int)quantity.Value, unitCost.Value));
        RefreshLines();
    }

    private void RemoveLine_Click(object? sender, EventArgs e)
    {
        if (lineGrid.CurrentRow == null || lineGrid.CurrentRow.Index < 0 || lineGrid.CurrentRow.Index >= lines.Count) return;
        lines.RemoveAt(lineGrid.CurrentRow.Index); RefreshLines();
    }

    private void RefreshLines()
    {
        lineGrid.Rows.Clear();
        foreach (var l in lines) lineGrid.Rows.Add(l.Code, l.Name, l.Quantity, l.UnitCost, l.Quantity * l.UnitCost);
        orderTotal.Text = $"ORDER TOTAL: ₱{lines.Sum(l => l.Quantity * l.UnitCost):N2}";
    }

    private void SaveOrder_Click(object? sender, EventArgs e)
    {
        if (supplier.SelectedItem is not Choice s) { MessageBox.Show("Select a supplier. Add one under SUPPLIERS first if the list is empty."); return; }
        if (lines.Count == 0) { MessageBox.Show("Add at least one product line."); return; }
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction();
            var poNumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            using var po = new MySqlCommand("INSERT INTO purchase_orders(po_number, supplier_id, status, notes, created_by) VALUES(@no,@supplier,'PENDING',@notes,@user);", c, tx);
            po.Parameters.AddWithValue("@no", poNumber); po.Parameters.AddWithValue("@supplier", s.Id);
            po.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes.Text) ? DBNull.Value : (object)notes.Text.Trim());
            po.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value);
            po.ExecuteNonQuery();
            var poId = Convert.ToInt32(po.LastInsertedId);
            foreach (var l in lines)
            {
                using var item = new MySqlCommand("INSERT INTO purchase_order_items(purchase_order_id, product_id, quantity, unit_cost) VALUES(@po,@product,@qty,@cost);", c, tx);
                item.Parameters.AddWithValue("@po", poId); item.Parameters.AddWithValue("@product", l.ProductId); item.Parameters.AddWithValue("@qty", l.Quantity); item.Parameters.AddWithValue("@cost", l.UnitCost);
                item.ExecuteNonQuery();
            }
            tx.Commit();
            MessageBox.Show($"Purchase order {poNumber} saved as PENDING.", "Order Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lines.Clear(); RefreshLines(); notes.Clear();
        }
        catch (Exception ex) { MessageBox.Show("The order was not saved.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ---------- Orders tab ----------

    private void BuildOrdersTab(TabPage tab)
    {
        orderGrid.Location = new Point(20, 20); orderGrid.Size = new Size(1015, 230); orderGrid.ReadOnly = true; orderGrid.AllowUserToAddRows = false; orderGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; orderGrid.MultiSelect = false; orderGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        orderGrid.SelectionChanged += (_, _) => LoadItems();
        tab.Controls.Add(orderGrid);

        var receive = Ui.Button("RECEIVE ORDER", 160); receive.Location = new Point(20, 262); receive.Click += Receive_Click; tab.Controls.Add(receive);
        var cancel = Ui.Button("CANCEL ORDER", 150); cancel.BackColor = Color.FromArgb(190, 55, 45); cancel.Location = new Point(195, 262); cancel.Click += Cancel_Click; tab.Controls.Add(cancel);
        var refresh = Ui.Button("REFRESH", 110); refresh.BackColor = Color.Gray; refresh.Location = new Point(360, 262); refresh.Click += (_, _) => LoadOrders(); tab.Controls.Add(refresh);

        tab.Controls.Add(new Label { Text = "Items in the selected order", Location = new Point(20, 318), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        itemGrid.Location = new Point(20, 342); itemGrid.Size = new Size(1015, 200); itemGrid.ReadOnly = true; itemGrid.AllowUserToAddRows = false; itemGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; itemGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tab.Controls.Add(itemGrid);
    }

    private void LoadOrders()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand(@"
                SELECT po.id, po.po_number AS 'PO No', s.supplier_name AS Supplier, po.status AS Status,
                       COUNT(i.id) AS Items, COALESCE(SUM(i.quantity * i.unit_cost), 0) AS Total,
                       po.created_at AS Created, po.received_at AS Received
                FROM purchase_orders po
                INNER JOIN suppliers s ON s.id = po.supplier_id
                LEFT JOIN purchase_order_items i ON i.purchase_order_id = po.id
                GROUP BY po.id, po.po_number, s.supplier_name, po.status, po.created_at, po.received_at
                ORDER BY po.created_at DESC, po.id DESC;", c);
            using var adapter = new MySqlDataAdapter(cmd); var table = new DataTable(); adapter.Fill(table);
            orderGrid.DataSource = table;
            if (orderGrid.Columns.Contains("id")) orderGrid.Columns["id"].Visible = false;
            if (orderGrid.Columns.Contains("Total")) orderGrid.Columns["Total"].DefaultCellStyle.Format = "₱#,##0.00";
            if (orderGrid.Columns.Contains("Created")) orderGrid.Columns["Created"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            if (orderGrid.Columns.Contains("Received")) orderGrid.Columns["Received"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            LoadItems();
        }
        catch (Exception ex) { MessageBox.Show("Could not load purchase orders.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private int? SelectedOrderId()
    {
        if (orderGrid.CurrentRow == null || orderGrid.CurrentRow.Index < 0 || !orderGrid.Columns.Contains("id")) return null;
        var value = orderGrid.CurrentRow.Cells["id"].Value;
        return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    private string? SelectedStatus() => orderGrid.CurrentRow == null || !orderGrid.Columns.Contains("Status") ? null : Convert.ToString(orderGrid.CurrentRow.Cells["Status"].Value);

    private void LoadItems()
    {
        var id = SelectedOrderId();
        if (id == null) { itemGrid.DataSource = null; return; }
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand(@"
                SELECT p.product_id AS 'Product ID', p.product_name AS Product, i.quantity AS Quantity,
                       i.unit_cost AS 'Unit Cost', i.quantity * i.unit_cost AS 'Line Total'
                FROM purchase_order_items i INNER JOIN products p ON p.id = i.product_id
                WHERE i.purchase_order_id=@id ORDER BY p.product_name;", c);
            cmd.Parameters.AddWithValue("@id", id.Value);
            using var adapter = new MySqlDataAdapter(cmd); var table = new DataTable(); adapter.Fill(table);
            itemGrid.DataSource = table;
            foreach (var money in new[] { "Unit Cost", "Line Total" })
                if (itemGrid.Columns.Contains(money)) itemGrid.Columns[money].DefaultCellStyle.Format = "₱#,##0.00";
        }
        catch { itemGrid.DataSource = null; }
    }

    private void Receive_Click(object? sender, EventArgs e)
    {
        var id = SelectedOrderId();
        if (id == null) { MessageBox.Show("Select an order first."); return; }
        if (SelectedStatus() != "PENDING") { MessageBox.Show("Only PENDING orders can be received."); return; }
        if (MessageBox.Show("Mark this order as received and add its items to stock?", "Receive Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction();

            string poNumber;
            using (var check = new MySqlCommand("SELECT po_number, status FROM purchase_orders WHERE id=@id FOR UPDATE", c, tx))
            {
                check.Parameters.AddWithValue("@id", id.Value);
                using var r = check.ExecuteReader();
                if (!r.Read()) throw new InvalidOperationException("The order no longer exists.");
                poNumber = r.GetString("po_number");
                if (r.GetString("status") != "PENDING") throw new InvalidOperationException("This order was already received or cancelled.");
            }

            var items = new List<(int ProductId, int Qty, decimal Cost)>();
            using (var read = new MySqlCommand("SELECT product_id, quantity, unit_cost FROM purchase_order_items WHERE purchase_order_id=@id", c, tx))
            {
                read.Parameters.AddWithValue("@id", id.Value);
                using var r = read.ExecuteReader();
                while (r.Read()) items.Add((r.GetInt32("product_id"), r.GetInt32("quantity"), r.GetDecimal("unit_cost")));
            }

            foreach (var item in items)
            {
                // Add the delivered quantity and keep the latest purchase price as the product's cost.
                using var update = new MySqlCommand("UPDATE products SET quantity = quantity + @qty, cost_price = @cost WHERE id=@id", c, tx);
                update.Parameters.AddWithValue("@qty", item.Qty); update.Parameters.AddWithValue("@cost", item.Cost); update.Parameters.AddWithValue("@id", item.ProductId);
                if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("A product in this order no longer exists.");

                using var movement = new MySqlCommand(@"
                    INSERT INTO inventory_movements(product_id, movement_type, quantity, reference_type, reference_id, notes, user_id)
                    VALUES(@product,'RESTOCK',@qty,'PURCHASE_ORDER',@po,@notes,@user);", c, tx);
                movement.Parameters.AddWithValue("@product", item.ProductId); movement.Parameters.AddWithValue("@qty", item.Qty); movement.Parameters.AddWithValue("@po", id.Value);
                movement.Parameters.AddWithValue("@notes", "Received " + poNumber); movement.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value);
                movement.ExecuteNonQuery();
            }

            using var done = new MySqlCommand("UPDATE purchase_orders SET status='RECEIVED', received_at=NOW() WHERE id=@id", c, tx);
            done.Parameters.AddWithValue("@id", id.Value); done.ExecuteNonQuery();
            tx.Commit();
            MessageBox.Show($"{poNumber} received. Stock and movement history were updated.", "Order Received", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadOrders();
        }
        catch (Exception ex) { MessageBox.Show("The order could not be received. No stock was changed.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Cancel_Click(object? sender, EventArgs e)
    {
        var id = SelectedOrderId();
        if (id == null) { MessageBox.Show("Select an order first."); return; }
        if (SelectedStatus() != "PENDING") { MessageBox.Show("Only PENDING orders can be cancelled."); return; }
        if (MessageBox.Show("Cancel this purchase order?", "Cancel Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand("UPDATE purchase_orders SET status='CANCELLED' WHERE id=@id AND status='PENDING'", c);
            cmd.Parameters.AddWithValue("@id", id.Value);
            if (cmd.ExecuteNonQuery() != 1) MessageBox.Show("This order is no longer pending.");
            LoadOrders();
        }
        catch (Exception ex) { MessageBox.Show("Could not cancel the order.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ---------- helper types ----------

    private sealed record Choice(int Id, string Text, decimal Cost, string Code = "", string ProductName = "")
    {
        public override string ToString() => Text;
    }

    private sealed class OrderLine
    {
        public int ProductId { get; } public string Code { get; } public string Name { get; } public int Quantity { get; set; } public decimal UnitCost { get; set; }
        public OrderLine(int productId, string code, string name, int quantity, decimal unitCost) { ProductId = productId; Code = code; Name = name; Quantity = quantity; UnitCost = unitCost; }
    }
}
