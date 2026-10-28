using CIT_Inventory_Management_System.Database;
using MySql.Data.MySqlClient;

namespace CIT_Inventory_Management_System;

public class RegisterForm : Form
{
    private readonly TextBox fullName = new();
    private readonly TextBox username = new();
    private readonly TextBox password = new();
    private readonly TextBox confirmPassword = new();

    public RegisterForm()
    {
        Text = "Register Staff";
        ClientSize = new Size(520, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var logo = Ui.Logo(75); logo.Location = new Point(222, 10); Controls.Add(logo);
        var title = Ui.Title("CREATE STAFF ACCOUNT", 17); title.Location = new Point(130, 90); Controls.Add(title);

        Add("Full Name", fullName, 125);
        Add("Username", username, 190);
        Add("Password", password, 255);
        Add("Confirm Password", confirmPassword, 320);
        password.PasswordChar = confirmPassword.PasswordChar = '●';

        var create = Ui.Button("CREATE ACCOUNT", 180); create.Location = new Point(45, 410); create.Click += Create_Click; Controls.Add(create);
        var cancel = Ui.Button("CANCEL", 120); cancel.BackColor = Color.Gray; cancel.Location = new Point(240, 410); cancel.Click += (_, _) => Close(); Controls.Add(cancel);
    }

    private void Add(string label, TextBox box, int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(45, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        box.Location = new Point(45, y + 22); box.Width = 410; Controls.Add(box);
    }

    private void Create_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(fullName.Text) || string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(password.Text))
        {
            MessageBox.Show("Please complete all required fields.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
        }
        if (password.Text != confirmPassword.Text)
        {
            MessageBox.Show("Passwords do not match.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
        }

        try
        {
            using var connection = DatabaseConnection.GetConnection(); connection.Open();
            using var cmd = new MySqlCommand(@"
                INSERT INTO users(full_name, username, password_hash)
                VALUES(@name, @username, @password);", connection);
            cmd.Parameters.AddWithValue("@name", fullName.Text.Trim());
            cmd.Parameters.AddWithValue("@username", username.Text.Trim());
            cmd.Parameters.AddWithValue("@password", Security.HashPassword(password.Text));
            cmd.ExecuteNonQuery();
            MessageBox.Show("Staff account created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            MessageBox.Show("That username is already registered.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not create the account.\n\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
