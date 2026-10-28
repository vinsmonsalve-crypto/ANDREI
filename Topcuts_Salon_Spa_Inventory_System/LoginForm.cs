using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;

namespace CIT_Inventory_Management_System;

public class LoginForm : Form
{
    private readonly TextBox username = new();
    private readonly TextBox password = new();

    public LoginForm()
    {
        Text = "Topcuts Salon & Spa Inventory System - Login";
        ClientSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Ui.Green };
        var logo = Ui.Logo(68); logo.Location = new Point(25, 12); header.Controls.Add(logo);
        var title = new Label { Text = "TOPCUTS SALON & SPA INVENTORY SYSTEM", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Size = new Size(750, 54), Location = new Point(110, 19) };
        header.Controls.Add(title);
        Controls.Add(header);

        var card = new Panel { Location = new Point(260, 125), Size = new Size(380, 350), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var cardLogo = Ui.Logo(82); cardLogo.Location = new Point(149, 15); card.Controls.Add(cardLogo);
        var welcome = Ui.Title("Staff Login", 18); welcome.Location = new Point(130, 100); card.Controls.Add(welcome);

        AddField(card, "Username", username, 140);
        AddField(card, "Password", password, 200);
        password.PasswordChar = '●';

        var login = Ui.Button("LOGIN", 140); login.Location = new Point(45, 280); login.Click += Login_Click; card.Controls.Add(login);
        var register = Ui.Button("REGISTER", 140); register.BackColor = Ui.Gold; register.ForeColor = Color.Black; register.Location = new Point(195, 280); register.Click += (_, _) => new RegisterForm().ShowDialog(this); card.Controls.Add(register);
        Controls.Add(card);

        AcceptButton = login;
    }

    private static void AddField(Control parent, string label, TextBox box, int y)
    {
        parent.Controls.Add(new Label { Text = label, Location = new Point(45, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        box.Location = new Point(45, y + 22); box.Width = 290; box.Height = 30;
        parent.Controls.Add(box);
    }

    private void Login_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(password.Text))
        {
            MessageBox.Show("Please enter your username and password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand(@"
                SELECT id, full_name, username
                FROM users
                WHERE username = @username AND password_hash = @password
                LIMIT 1;", connection);
            cmd.Parameters.AddWithValue("@username", username.Text.Trim());
            cmd.Parameters.AddWithValue("@password", Security.HashPassword(password.Text));

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Session.UserId = reader.GetInt32("id");
            Session.FullName = reader.GetString("full_name");
            Session.Username = reader.GetString("username");

            Hide();
            using var dashboard = new DashboardForm();
            dashboard.ShowDialog();
            Session.Clear();
            Show();
            password.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Login failed.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
