namespace CIT_Inventory_Management_System.Database;

/// <summary>
/// MySQL settings used by the whole application.
/// These defaults match normal XAMPP/WAMP MySQL installation.
/// </summary>
public static class DatabaseSettings
{
    // XAMPP/WAMP normally uses localhost (127.0.0.1) and port 3307.
    public const string Server = "127.0.0.1";
    public const uint Port = 3307;

    // Default XAMPP/WAMP MySQL account.
    public const string Username = "root";
    public const string Password = "";

    // The application creates this database automatically if it does not exist.
    public const string DatabaseName = "topcuts_salon_inventory";

    public static string ServerDescription => $"{Server}:{Port}";
}
