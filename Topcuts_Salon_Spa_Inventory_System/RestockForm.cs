using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;

namespace CIT_Inventory_Management_System;

public class RestockForm : Form
{
    private readonly ComboBox product = new();
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 100000 };
    private readonly Label currentStock = new();

    public RestockForm()
    {
        Text = "Restock Inventory"; ClientSize = new Size(650, 420); StartPosition = FormStartPosition.CenterParent; BackColor = Color.White;
        var logo = Ui.Logo(85); logo.Location = new Point(280, 10); Controls.Add(logo);
        var title = Ui.Title("RESTOCK INVENTORY", 19); title.Location = new Point(205, 105); Controls.Add(title);
        Controls.Add(new Label { Text = "Product", Location = new Point(70, 160), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        product.Location = new Point(180, 156); product.Width = 390; product.DropDownStyle = ComboBoxStyle.DropDownList; product.SelectedIndexChanged += (_, _) => UpdateStockLabel(); Controls.Add(product);
        Controls.Add(new Label { Text = "Current Stock", Location = new Point(70, 210), AutoSize = true });
        currentStock.Location = new Point(180, 210); currentStock.AutoSize = true; currentStock.Font = new Font("Segoe UI", 11, FontStyle.Bold); currentStock.ForeColor = Ui.Green; Controls.Add(currentStock);
        Controls.Add(new Label { Text = "Quantity to Add", Location = new Point(70, 255), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        quantity.Location = new Point(180, 250); quantity.Width = 160; Controls.Add(quantity);
        var restock = Ui.Button("RESTOCK", 150); restock.Location = new Point(180, 310); restock.Click += Restock_Click; Controls.Add(restock);
        var close = Ui.Button("CLOSE", 120); close.BackColor = Color.Gray; close.Location = new Point(345, 310); close.Click += (_, _) => Close(); Controls.Add(close);
        Load += (_, _) => LoadProducts();
    }

    private void LoadProducts()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand("SELECT id, product_name FROM products WHERE is_active=1 ORDER BY product_name", c);
            using var r = cmd.ExecuteReader();
            while (r.Read()) product.Items.Add(new ProductChoice(r.GetInt32("id"), r.GetString("product_name")));
            if (product.Items.Count > 0) product.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show("Could not load products.\n\n" + ex.Message); }
    }

    private void UpdateStockLabel()
    {
        if (product.SelectedItem is not ProductChoice choice) { currentStock.Text = "—"; return; }
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var cmd = new MySqlCommand("SELECT quantity FROM products WHERE id=@id", c); cmd.Parameters.AddWithValue("@id", choice.Id); currentStock.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";
        }
        catch { currentStock.Text = "—"; }
    }

    private void Restock_Click(object? sender, EventArgs e)
    {
        if (product.SelectedItem is not ProductChoice choice) { MessageBox.Show("Select a product."); return; }
        quantity.Value = quantity.Value; // commit any text still being typed in the box
        var amount = (int)quantity.Value;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction();

            // Lock the row and read the current stock so we can set an exact new value and verify it.
            using var read = new MySqlCommand("SELECT quantity FROM products WHERE id=@id AND is_active=1 FOR UPDATE", c, tx); read.Parameters.AddWithValue("@id", choice.Id);
            var before = read.ExecuteScalar();
            if (before == null) throw new InvalidOperationException("The product is no longer available.");
            var expected = Convert.ToInt32(before) + amount;

            using var update = new MySqlCommand("UPDATE products SET quantity=@new WHERE id=@id", c, tx); update.Parameters.AddWithValue("@new", expected); update.Parameters.AddWithValue("@id", choice.Id); update.ExecuteNonQuery();

            // Safety check: if anything in the database (e.g. a leftover trigger) changed the stock
            // by a different amount than requested, roll back instead of saving wrong numbers.
            using var verify = new MySqlCommand("SELECT quantity FROM products WHERE id=@id", c, tx); verify.Parameters.AddWithValue("@id", choice.Id);
            var after = Convert.ToInt32(verify.ExecuteScalar());
            if (after != expected)
                throw new InvalidOperationException($"Stock would have become {after} instead of {expected}. A database trigger on the products table is probably modifying the quantity. Nothing was saved.");

            using var movement = new MySqlCommand(@"
                INSERT INTO inventory_movements(product_id,movement_type,quantity,reference_type,notes,user_id)
                VALUES(@product,'RESTOCK',@qty,'RESTOCK','Manual restock',@user);", c, tx);
            movement.Parameters.AddWithValue("@product", choice.Id); movement.Parameters.AddWithValue("@qty", amount); movement.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value); movement.ExecuteNonQuery();
            tx.Commit();
            MessageBox.Show("Restock saved successfully. The stock and movement history were updated together.", "Restock Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            quantity.Value = 1; UpdateStockLabel();
        }
        catch (Exception ex) { MessageBox.Show("Restock failed. No partial stock change was saved.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private sealed record ProductChoice(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}
