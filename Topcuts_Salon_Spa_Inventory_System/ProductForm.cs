using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;
using System.Data;

namespace CIT_Inventory_Management_System;

public class ProductForm : Form
{
    private readonly TextBox productId = new();
    private readonly TextBox productName = new();
    private readonly ComboBox category = new();
    private readonly NumericUpDown price = new() { DecimalPlaces = 2, Maximum = 1000000, Minimum = 0 };
    private readonly NumericUpDown cost = new() { DecimalPlaces = 2, Maximum = 1000000, Minimum = 0 };
    private readonly NumericUpDown quantity = new() { Maximum = 1000000, Minimum = 0 };
    private readonly NumericUpDown lowStock = new() { Maximum = 100000, Minimum = 0, Value = 10 };
    private readonly DataGridView grid = new();
    private int? selectedId;

    public ProductForm()
    {
        Text = "Product Inventory"; ClientSize = new Size(1100, 700); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(245, 247, 252);
        var title = Ui.Title("PRODUCT INVENTORY", 21); title.Location = new Point(30, 22); Controls.Add(title);
        var logo = Ui.Logo(70); logo.Location = new Point(1000, 10); Controls.Add(logo);

        AddField("Product ID", productId, 30, 80, 130);
        AddField("Product Name", productName, 175, 80, 180);
        category.Items.AddRange(new object[] { "Shampoo", "Conditioner", "Hair Treatment", "Hair Color", "Styling Product", "Skin Care", "Nail Care", "Salon Equipment", "Spa Supplies", "Other" }); category.DropDownStyle = ComboBoxStyle.DropDownList;
        AddField("Category", category, 370, 80, 150);
        AddField("Cost Price", cost, 535, 80, 110);
        AddField("Selling Price", price, 660, 80, 110);
        AddField("Quantity", quantity, 785, 80, 90);
        AddField("Low Stock", lowStock, 890, 80, 100);

        var save = Ui.Button("SAVE", 100); save.Location = new Point(30, 145); save.Click += Save_Click; Controls.Add(save);
        var update = Ui.Button("UPDATE", 100); update.Location = new Point(140, 145); update.Click += Update_Click; Controls.Add(update);
        var delete = Ui.Button("DELETE", 115); delete.BackColor = Color.FromArgb(190, 55, 45); delete.Location = new Point(250, 145); delete.Click += Delete_Click; Controls.Add(delete);
        var clear = Ui.Button("CLEAR", 100); clear.BackColor = Color.Gray; clear.Location = new Point(375, 145); clear.Click += (_, _) => ClearFields(); Controls.Add(clear);

        grid.Location = new Point(30, 205); grid.Size = new Size(1035, 430); grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellClick += Grid_CellClick; Controls.Add(grid);
        Load += (_, _) => LoadProducts();
    }

    private void AddField(string label, Control control, int x, int y, int width)
    {
        Controls.Add(new Label { Text = label, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) });
        control.Location = new Point(x, y + 23); control.Width = width; Controls.Add(control);
    }

    private void LoadProducts()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand(@"
                SELECT id, product_id AS 'Product ID', product_name AS 'Product Name', category AS Category,
                       cost_price AS Cost, price AS Price, (price - cost_price) AS 'Profit/Unit', quantity AS Quantity, low_stock_threshold AS 'Low Stock',
                       CASE WHEN quantity <= low_stock_threshold THEN 'LOW STOCK' ELSE 'IN STOCK' END AS Status
                FROM products WHERE is_active=1 ORDER BY product_name;", c);
            using var adapter = new MySqlDataAdapter(cmd); var table = new DataTable(); adapter.Fill(table);
            grid.DataSource = table;
            if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
            foreach (var money in new[] { "Cost", "Price", "Profit/Unit" })
                if (grid.Columns.Contains(money)) grid.Columns[money].DefaultCellStyle.Format = "₱#,##0.00";
        }
        catch (Exception ex) { MessageBox.Show("Could not load products.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var row = grid.Rows[e.RowIndex];
        selectedId = Convert.ToInt32(row.Cells["id"].Value);
        productId.Text = Convert.ToString(row.Cells["Product ID"].Value) ?? "";
        productName.Text = Convert.ToString(row.Cells["Product Name"].Value) ?? "";
        category.Text = Convert.ToString(row.Cells["Category"].Value) ?? "";
        price.Value = Convert.ToDecimal(row.Cells["Price"].Value);
        cost.Value = Convert.ToDecimal(row.Cells["Cost"].Value);
        quantity.Value = Convert.ToDecimal(row.Cells["Quantity"].Value);
        lowStock.Value = Convert.ToDecimal(row.Cells["Low Stock"].Value);
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        if (!ValidateProduct()) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction();
            using var cmd = new MySqlCommand(@"
                INSERT INTO products(product_id, product_name, category, price, cost_price, quantity, low_stock_threshold)
                VALUES(@id,@name,@category,@price,@cost,@quantity,@low);", c, tx);
            AddParameters(cmd); cmd.ExecuteNonQuery();
            var newId = Convert.ToInt32(cmd.LastInsertedId);
            if (quantity.Value > 0) InsertMovement(c, tx, newId, "RESTOCK", (int)quantity.Value, null, null, "Initial stock");
            tx.Commit();
            MessageBox.Show("Product saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); ClearFields(); LoadProducts();
        }
        catch (MySqlException ex) when (ex.Number == 1062) { MessageBox.Show("That Product ID already exists.", "Duplicate Product ID", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        catch (Exception ex) { MessageBox.Show("Could not save the product.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Update_Click(object? sender, EventArgs e)
    {
        if (selectedId == null) { MessageBox.Show("Select a product first."); return; }
        if (!ValidateProduct()) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var tx = c.BeginTransaction();
            using var lockCmd = new MySqlCommand("SELECT quantity FROM products WHERE id=@id FOR UPDATE", c, tx); lockCmd.Parameters.AddWithValue("@id", selectedId.Value);
            var oldQty = Convert.ToInt32(lockCmd.ExecuteScalar());
            using var cmd = new MySqlCommand(@"
                UPDATE products SET product_id=@pid, product_name=@name, category=@category, price=@price, cost_price=@cost,
                    quantity=@quantity, low_stock_threshold=@low WHERE id=@id;", c, tx);
            cmd.Parameters.AddWithValue("@pid", productId.Text.Trim()); cmd.Parameters.AddWithValue("@name", productName.Text.Trim()); cmd.Parameters.AddWithValue("@category", category.Text); cmd.Parameters.AddWithValue("@price", price.Value); cmd.Parameters.AddWithValue("@cost", cost.Value); cmd.Parameters.AddWithValue("@quantity", quantity.Value); cmd.Parameters.AddWithValue("@low", lowStock.Value); cmd.Parameters.AddWithValue("@id", selectedId.Value); cmd.ExecuteNonQuery();
            var difference = (int)quantity.Value - oldQty;
            if (difference != 0) InsertMovement(c, tx, selectedId.Value, difference > 0 ? "RESTOCK" : "ADJUSTMENT", Math.Abs(difference), "PRODUCT_UPDATE", selectedId.Value, "Manual inventory adjustment");
            tx.Commit();
            MessageBox.Show("Product updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); ClearFields(); LoadProducts();
        }
        catch (MySqlException ex) when (ex.Number == 1062) { MessageBox.Show("That Product ID already exists.", "Duplicate Product ID", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        catch (Exception ex) { MessageBox.Show("Could not update the product.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Delete_Click(object? sender, EventArgs e)
    {
        if (selectedId == null) { MessageBox.Show("Select a product first."); return; }
        if (MessageBox.Show("Delete this product? Its sales history will be preserved.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open(); using var cmd = new MySqlCommand("UPDATE products SET is_active=0 WHERE id=@id", c); cmd.Parameters.AddWithValue("@id", selectedId.Value); cmd.ExecuteNonQuery();
            ClearFields(); LoadProducts();
        }
        catch (Exception ex) { MessageBox.Show("Could not delete the product.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private bool ValidateProduct()
    {
        if (string.IsNullOrWhiteSpace(productId.Text) || string.IsNullOrWhiteSpace(productName.Text) || string.IsNullOrWhiteSpace(category.Text)) { MessageBox.Show("Product ID, name, and category are required."); return false; }
        return true;
    }

    private void AddParameters(MySqlCommand cmd)
    {
        cmd.Parameters.AddWithValue("@id", productId.Text.Trim()); cmd.Parameters.AddWithValue("@name", productName.Text.Trim()); cmd.Parameters.AddWithValue("@category", category.Text); cmd.Parameters.AddWithValue("@price", price.Value); cmd.Parameters.AddWithValue("@cost", cost.Value); cmd.Parameters.AddWithValue("@quantity", quantity.Value); cmd.Parameters.AddWithValue("@low", lowStock.Value);
    }

    private static void InsertMovement(MySqlConnection c, MySqlTransaction tx, int productId, string type, int quantity, string? referenceType, int? referenceId, string notes)
    {
        using var cmd = new MySqlCommand(@"
            INSERT INTO inventory_movements(product_id, movement_type, quantity, reference_type, reference_id, notes, user_id)
            VALUES(@product,@type,@quantity,@refType,@refId,@notes,@user);", c, tx);
        cmd.Parameters.AddWithValue("@product", productId); cmd.Parameters.AddWithValue("@type", type); cmd.Parameters.AddWithValue("@quantity", quantity); cmd.Parameters.AddWithValue("@refType", (object?)referenceType ?? DBNull.Value); cmd.Parameters.AddWithValue("@refId", (object?)referenceId ?? DBNull.Value); cmd.Parameters.AddWithValue("@notes", notes); cmd.Parameters.AddWithValue("@user", (object?)Session.UserId ?? DBNull.Value); cmd.ExecuteNonQuery();
    }

    private void ClearFields()
    {
        selectedId = null; productId.Clear(); productName.Clear(); category.SelectedIndex = -1; price.Value = 0; cost.Value = 0; quantity.Value = 0; lowStock.Value = 10; grid.ClearSelection();
    }
}
