using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;
using System.Data;

namespace CIT_Inventory_Management_System;

public class SalesForm : Form
{
    private readonly ComboBox product = new();
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 100000 };
    private readonly Label stock = new();
    private readonly DataGridView cart = new();
    private readonly Label grandTotal = new();
    private readonly List<CartItem> items = new();

    public SalesForm()
    {
        Text = "Sales Transaction"; ClientSize = new Size(1050, 680); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(248, 250, 247);
        var title = Ui.Title("SALES TRANSACTION", 21); title.Location = new Point(30, 20); Controls.Add(title);
        var logo = Ui.Logo(65); logo.Location = new Point(955, 8); Controls.Add(logo);

        Controls.Add(new Label { Text = "Product", Location = new Point(30, 82), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        product.Location = new Point(100, 78); product.Width = 360; product.DropDownStyle = ComboBoxStyle.DropDownList; product.SelectedIndexChanged += (_, _) => UpdateStock(); Controls.Add(product);
        Controls.Add(new Label { Text = "Available", Location = new Point(480, 82), AutoSize = true }); stock.Location = new Point(550, 82); stock.AutoSize = true; stock.Font = new Font("Segoe UI", 10, FontStyle.Bold); stock.ForeColor = Ui.Green; Controls.Add(stock);
        Controls.Add(new Label { Text = "Quantity", Location = new Point(30, 125), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) }); quantity.Location = new Point(100, 120); quantity.Width = 120; Controls.Add(quantity);
        var add = Ui.Button("ADD TO CART", 145); add.Location = new Point(250, 115); add.Click += AddToCart; Controls.Add(add);
        var clearCart = Ui.Button("CLEAR CART", 130); clearCart.BackColor = Color.Gray; clearCart.Location = new Point(405, 115); clearCart.Click += (_, _) => { items.Clear(); RefreshCart(); }; Controls.Add(clearCart);

        cart.Location = new Point(30, 175); cart.Size = new Size(990, 350); cart.ReadOnly = true; cart.AllowUserToAddRows = false; cart.SelectionMode = DataGridViewSelectionMode.FullRowSelect; cart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        cart.Columns.Add("productId", "Product ID"); cart.Columns.Add("name", "Product"); cart.Columns.Add("qty", "Qty"); cart.Columns.Add("price", "Unit Price"); cart.Columns.Add("total", "Total");
        Controls.Add(cart);
        grandTotal.Text = "TOTAL: ₱0.00"; grandTotal.Location = new Point(700, 545); grandTotal.AutoSize = true; grandTotal.Font = new Font("Segoe UI", 17, FontStyle.Bold); grandTotal.ForeColor = Ui.Green; Controls.Add(grandTotal);
        var complete = Ui.Button("COMPLETE SALE", 180); complete.Location = new Point(700, 585); complete.Click += CompleteSale; Controls.Add(complete);
        var remove = Ui.Button("REMOVE ITEM", 140); remove.BackColor = Color.FromArgb(190, 55, 45); remove.Location = new Point(545, 585); remove.Click += RemoveItem; Controls.Add(remove);
        Load += (_, _) => LoadProducts();
    }

    private void LoadProducts()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var cmd = new MySqlCommand("SELECT id, product_id, product_name, price, quantity FROM products WHERE is_active=1 ORDER BY product_name", c); using var r = cmd.ExecuteReader();
            while (r.Read()) product.Items.Add(new ProductChoice(r.GetInt32("id"), r.GetString("product_id"), r.GetString("product_name"), r.GetDecimal("price"), r.GetInt32("quantity")));
            if (product.Items.Count > 0) product.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show("Could not load products.\n\n" + ex.Message); }
    }

    private void UpdateStock()
    {
        if (product.SelectedItem is ProductChoice choice) stock.Text = choice.Quantity.ToString(); else stock.Text = "—";
    }

    private void AddToCart(object? sender, EventArgs e)
    {
        if (product.SelectedItem is not ProductChoice choice) { MessageBox.Show("Select a product."); return; }
        var existing = items.FirstOrDefault(x => x.ProductId == choice.Id);
        var requested = (existing?.Quantity ?? 0) + (int)quantity.Value;
        if (requested > choice.Quantity) { MessageBox.Show($"Only {choice.Quantity} unit(s) are available for {choice.Name}.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (existing == null) items.Add(new CartItem(choice.Id, choice.Code, choice.Name, choice.Price, (int)quantity.Value)); else existing.Quantity = requested;
        RefreshCart();
    }

    private void RefreshCart()
    {
        cart.Rows.Clear();
        decimal total = 0;
        foreach (var item in items) { var line = item.Price * item.Quantity; total += line; cart.Rows.Add(item.Code, item.Name, item.Quantity, item.Price, line); }
        if (cart.Columns.Count > 0) { cart.Columns[3].DefaultCellStyle.Format = "₱#,##0.00"; cart.Columns[4].DefaultCellStyle.Format = "₱#,##0.00"; }
        grandTotal.Text = $"TOTAL: ₱{total:N2}";
    }

    private void RemoveItem(object? sender, EventArgs e)
    {
        if (cart.CurrentRow == null || cart.CurrentRow.Index < 0 || cart.CurrentRow.Index >= items.Count) return;
        items.RemoveAt(cart.CurrentRow.Index); RefreshCart();
    }

    private void CompleteSale(object? sender, EventArgs e)
    {
        if (items.Count == 0) { MessageBox.Show("Add at least one item to the cart."); return; }
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.ReadCommitted);
            decimal total = 0;
            var lockedProducts = new Dictionary<int, (string Code, string Name, decimal Price, decimal Cost, int Quantity)>();
            foreach (var item in items)
            {
                using var lockCmd = new MySqlCommand(@"SELECT product_id, product_name, price, cost_price, quantity FROM products WHERE id=@id AND is_active=1 FOR UPDATE;", c, tx); lockCmd.Parameters.AddWithValue("@id", item.ProductId);
                using var reader = lockCmd.ExecuteReader();
                if (!reader.Read()) throw new InvalidOperationException($"Product {item.Name} is no longer active.");
                var available = reader.GetInt32("quantity"); var dbPrice = reader.GetDecimal("price"); var dbCost = reader.GetDecimal("cost_price"); var code = reader.GetString("product_id"); var name = reader.GetString("product_name"); reader.Close();
                if (item.Quantity > available) throw new InvalidOperationException($"Not enough stock for {name}. Available: {available}, requested: {item.Quantity}.");
                lockedProducts[item.ProductId] = (code, name, dbPrice, dbCost, available); total += dbPrice * item.Quantity;
            }

            var invoice = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            using var saleCmd = new MySqlCommand("INSERT INTO sales(invoice_no,user_id,total) VALUES(@invoice,@user,@total);", c, tx); saleCmd.Parameters.AddWithValue("@invoice", invoice); saleCmd.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value); saleCmd.Parameters.AddWithValue("@total", total); saleCmd.ExecuteNonQuery(); var saleId = Convert.ToInt32(saleCmd.LastInsertedId);

            foreach (var item in items)
            {
                var p = lockedProducts[item.ProductId]; var lineTotal = p.Price * item.Quantity;
                using var itemCmd = new MySqlCommand(@"INSERT INTO sale_items(sale_id,product_id,quantity,unit_price,unit_cost,total) VALUES(@sale,@product,@qty,@price,@cost,@total);", c, tx);
                itemCmd.Parameters.AddWithValue("@sale", saleId); itemCmd.Parameters.AddWithValue("@product", item.ProductId); itemCmd.Parameters.AddWithValue("@qty", item.Quantity); itemCmd.Parameters.AddWithValue("@price", p.Price); itemCmd.Parameters.AddWithValue("@cost", p.Cost); itemCmd.Parameters.AddWithValue("@total", lineTotal); itemCmd.ExecuteNonQuery();
                using var update = new MySqlCommand("UPDATE products SET quantity = quantity - @qty WHERE id=@id AND quantity >= @qty", c, tx); update.Parameters.AddWithValue("@qty", item.Quantity); update.Parameters.AddWithValue("@id", item.ProductId); if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Stock changed while completing the sale. The transaction was cancelled.");
                using var movement = new MySqlCommand(@"INSERT INTO inventory_movements(product_id,movement_type,quantity,reference_type,reference_id,notes,user_id) VALUES(@product,'SALE',@qty,'SALE',@sale,'Sold item',@user);", c, tx); movement.Parameters.AddWithValue("@product", item.ProductId); movement.Parameters.AddWithValue("@qty", item.Quantity); movement.Parameters.AddWithValue("@sale", saleId); movement.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value); movement.ExecuteNonQuery();
            }
            tx.Commit();

            // Keep the sale details for the receipt before the cart is cleared.
            var receipt = new ReceiptData(invoice, DateTime.Now, string.IsNullOrWhiteSpace(Session.FullName) ? "Staff" : Session.FullName,
                items.Select(i => new ReceiptLine(i.Name, i.Quantity, lockedProducts[i.ProductId].Price)).ToList());
            items.Clear(); RefreshCart(); ReloadProducts();

            var print = MessageBox.Show($"Sale completed successfully.\n\nInvoice: {invoice}\nTotal: ₱{total:N2}\n\nWould you like to print a receipt?", "Sale Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (print == DialogResult.Yes)
            {
                try { ReceiptPrinter.ShowPreview(this, receipt); }
                catch (Exception printEx) { MessageBox.Show("The sale was saved, but the receipt could not be shown.\n\n" + printEx.Message, "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }
        catch (Exception ex) { MessageBox.Show("Sale was not saved. Any stock deduction was rolled back.\n\n" + ex.Message, "Sale Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ReloadProducts()
    {
        product.Items.Clear();
        LoadProducts();
    }

    private sealed class ProductChoice
    {
        public int Id { get; } public string Code { get; } public string Name { get; } public decimal Price { get; } public int Quantity { get; }
        public ProductChoice(int id, string code, string name, decimal price, int quantity) { Id = id; Code = code; Name = name; Price = price; Quantity = quantity; }
        public override string ToString() => $"{Code} - {Name} (₱{Price:N2})";
    }

    private sealed class CartItem
    {
        public int ProductId { get; } public string Code { get; } public string Name { get; } public decimal Price { get; } public int Quantity { get; set; }
        public CartItem(int productId, string code, string name, decimal price, int quantity) { ProductId = productId; Code = code; Name = name; Price = price; Quantity = quantity; }
    }
}
