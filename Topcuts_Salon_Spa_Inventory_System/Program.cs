using CIT_Inventory_Management_System.Database;

namespace CIT_Inventory_Management_System;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            DatabaseConnection.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The system could not connect to MySQL.\n\n" +
                "Start MySQL in XAMPP, WAMP, or the MySQL Server service, then check Database/DatabaseSettings.cs.\n\n" +
                DatabaseConnection.GetConnectionSummary() + "\n\n" +
                "The database is created automatically when the connection succeeds.\n\n" +
                "Details: " + ex.Message,
                "MySQL Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new LoginForm());
    }
}
