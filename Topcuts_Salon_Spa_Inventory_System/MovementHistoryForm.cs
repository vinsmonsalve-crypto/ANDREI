using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;
using System.Data;

namespace CIT_Inventory_Management_System;

public class MovementHistoryForm : Form
{
    private readonly DataGridView grid = new();

    public MovementHistoryForm()
    {
        Text = "Inventory Movement History";
        ClientSize = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(248, 250, 247);

        var title = Ui.Title("INVENTORY MOVEMENT HISTORY", 20); title.Location = new Point(30, 20); Controls.Add(title);
        var logo = Ui.Logo(65); logo.Location = new Point(1000, 8); Controls.Add(logo);

        grid.Location = new Point(30, 95); grid.Size = new Size(1035, 480); grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Controls.Add(grid);
        var refresh = Ui.Button("REFRESH", 120); refresh.Location = new Point(30, 590); refresh.Click += (_, _) => LoadHistory(); Controls.Add(refresh);
        var close = Ui.Button("CLOSE", 120); close.BackColor = Color.Gray; close.Location = new Point(165, 590); close.Click += (_, _) => Close(); Controls.Add(close);
        Load += (_, _) => LoadHistory();
    }

    private void LoadHistory()
    {
        try
        {
            using var c = DatabaseConnection.GetConnection(); c.Open();
            using var cmd = new MySqlCommand(@"
                SELECT im.id AS ID, p.product_id AS 'Product ID', p.product_name AS Product,
                       im.movement_type AS Movement, im.quantity AS Quantity,
                       COALESCE(im.reference_type, '') AS Reference, COALESCE(im.notes, '') AS Notes,
                       COALESCE(u.username, 'system') AS User, im.created_at AS 'Date/Time'
                FROM inventory_movements im
                INNER JOIN products p ON p.id = im.product_id
                LEFT JOIN users u ON u.id = im.user_id
                ORDER BY im.created_at DESC, im.id DESC;", c);
            using var adapter = new MySqlDataAdapter(cmd); var table = new DataTable(); adapter.Fill(table); grid.DataSource = table;
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
            if (grid.Columns.Contains("Date/Time")) grid.Columns["Date/Time"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
        }
        catch (Exception ex) { MessageBox.Show("Could not load movement history.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
