using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;
using System.Data;

namespace CIT_Inventory_Management_System;

public class SupplierForm : Form
{
    private readonly TextBox name = new();
    private readonly TextBox contact = new();
    private readonly TextBox phone = new();
    private readonly TextBox email = new();
    private readonly TextBox address = new();
    private readonly DataGridView grid = new();
    private int? selectedId;

    public SupplierForm()
    {
        Text = "Suppliers"; ClientSize = new Size(1100, 700); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(245, 247, 252);
        var title = Ui.Title("SUPPLIERS", 21); title.Location = new Point(30, 22); Controls.Add(title);
        var logo = Ui.Logo(70); logo.Location = new Point(1000, 10); Controls.Add(logo);

        AddField("Supplier Name", name, 30, 80, 250);
        AddField("Contact Person", contact, 300, 80, 200);
        AddField("Phone", phone, 520, 80, 150);
        AddField("Email", email, 690, 80, 200);
        AddField("Address", address, 30, 140, 860);

        var save = Ui.Button("SAVE", 100); save.Location = new Point(30, 205); save.Click += Save_Click; Controls.Add(save);
        var update = Ui.Button("UPDATE", 100); update.Location = new Point(140, 205); update.Click += Update_Click; Controls.Add(update);
        var delete = Ui.Button("DELETE", 115); delete.BackColor = Color.FromArgb(190, 55, 45); delete.Location = new Point(250, 205); delete.Click += Delete_Click; Controls.Add(delete);
        var clear = Ui.Button("CLEAR", 100); clear.BackColor = Color.Gray; clear.Location = new Point(375, 205); clear.Click += (_, _) => ClearFields(); Controls.Add(clear);

        grid.Location = new Point(30, 265); grid.Size = new Size(1035, 400); grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellClick += Grid_CellClick; Controls.Add(grid);
        Load += (_, _) => LoadSuppliers();
    }

    private void AddField(string label, Control control, int x, int y, int width)
    {
        Controls.Add(new Label { Text = label, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) });
        control.Location = new Point(x, y + 23); control.Width = width; Controls.Add(control);
    }

    private void LoadSuppliers()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand(@"
                SELECT id, supplier_name AS 'Supplier', COALESCE(contact_person,'') AS 'Contact Person', COALESCE(phone,'') AS Phone,
                       COALESCE(email,'') AS Email, COALESCE(address,'') AS Address
                FROM suppliers WHERE is_active=1 ORDER BY supplier_name;", c);
            using var adapter = new MySqlDataAdapter(cmd); var table = new DataTable(); adapter.Fill(table);
            grid.DataSource = table;
            if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
        }
        catch (Exception ex) { MessageBox.Show("Could not load suppliers.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var row = grid.Rows[e.RowIndex];
        selectedId = Convert.ToInt32(row.Cells["id"].Value);
        name.Text = Convert.ToString(row.Cells["Supplier"].Value) ?? "";
        contact.Text = Convert.ToString(row.Cells["Contact Person"].Value) ?? "";
        phone.Text = Convert.ToString(row.Cells["Phone"].Value) ?? "";
        email.Text = Convert.ToString(row.Cells["Email"].Value) ?? "";
        address.Text = Convert.ToString(row.Cells["Address"].Value) ?? "";
    }

    private bool ValidateSupplier()
    {
        if (string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("Supplier name is required."); return false; }
        return true;
    }

    private void AddParameters(MySqlCommand cmd)
    {
        cmd.Parameters.AddWithValue("@name", name.Text.Trim());
        cmd.Parameters.AddWithValue("@contact", NullIfEmpty(contact.Text));
        cmd.Parameters.AddWithValue("@phone", NullIfEmpty(phone.Text));
        cmd.Parameters.AddWithValue("@email", NullIfEmpty(email.Text));
        cmd.Parameters.AddWithValue("@address", NullIfEmpty(address.Text));
    }

    private static object NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? DBNull.Value : (object)text.Trim();

    private void Save_Click(object? sender, EventArgs e)
    {
        if (!ValidateSupplier()) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand("INSERT INTO suppliers(supplier_name, contact_person, phone, email, address) VALUES(@name,@contact,@phone,@email,@address);", c);
            AddParameters(cmd); cmd.ExecuteNonQuery();
            MessageBox.Show("Supplier saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); ClearFields(); LoadSuppliers();
        }
        catch (Exception ex) { MessageBox.Show("Could not save the supplier.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Update_Click(object? sender, EventArgs e)
    {
        if (selectedId == null) { MessageBox.Show("Select a supplier first."); return; }
        if (!ValidateSupplier()) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand("UPDATE suppliers SET supplier_name=@name, contact_person=@contact, phone=@phone, email=@email, address=@address WHERE id=@id;", c);
            AddParameters(cmd); cmd.Parameters.AddWithValue("@id", selectedId.Value); cmd.ExecuteNonQuery();
            MessageBox.Show("Supplier updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); ClearFields(); LoadSuppliers();
        }
        catch (Exception ex) { MessageBox.Show("Could not update the supplier.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Delete_Click(object? sender, EventArgs e)
    {
        if (selectedId == null) { MessageBox.Show("Select a supplier first."); return; }
        if (MessageBox.Show("Delete this supplier? Its past purchase orders will be preserved.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand("UPDATE suppliers SET is_active=0 WHERE id=@id", c); cmd.Parameters.AddWithValue("@id", selectedId.Value); cmd.ExecuteNonQuery();
            ClearFields(); LoadSuppliers();
        }
        catch (Exception ex) { MessageBox.Show("Could not delete the supplier.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ClearFields()
    {
        selectedId = null; name.Clear(); contact.Clear(); phone.Clear(); email.Clear(); address.Clear(); grid.ClearSelection();
    }
}
