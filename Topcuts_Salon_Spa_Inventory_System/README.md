# Topcuts Salon & Spa Inventory System

Windows Forms + C# .NET 8 + MySQL inventory system for Topcuts Salon & Spa.

## Improvements in this version
- Added the supplied Topcuts Salon & Spa logo to the login, dashboard, inventory, sales, and restock screens.
- Added database-backed login and staff registration.
- Starts with an empty, separate `topcuts_salon_inventory` database so you can enter your own salon and spa products.
- Added product CRUD/deactivation with unique Product IDs.
- Added real database-backed sales transactions.
- Sales now lock the selected product rows and deduct stock inside a single MySQL transaction.
- Restocking updates stock and creates an inventory movement in the same transaction.
- Every stock addition/deduction is written to `inventory_movements`.
- Failed sales/restocks are rolled back so the database cannot be left half-updated.
- Dashboard values are loaded from MySQL instead of hard-coded numbers.
- Added low-stock thresholds and low-stock dashboard count.
- Added a complete SQL schema in `SQL/create_database.sql`.
- Added a dedicated `Database/DatabaseSettings.cs` file so the MySQL server, port, username, password, and database name are easy to change.
- The program checks the MySQL connection before opening the login screen and gives a clear error if MySQL is not running.

## Requirements
- Windows 10/11
- Visual Studio 2022/2026 with .NET desktop development
- .NET 8 SDK
- MySQL Server, XAMPP, or WAMP

## MySQL / XAMPP / WAMP settings
The project is already configured for the usual local MySQL setup:

| Setting | Default |
|---|---|
| Server | `127.0.0.1` |
| Port | `3307` |
| Username | `root` |
| Password | *(empty)* |
| Database | `topcuts_salon_inventory` |

These settings are in:

`Database/DatabaseSettings.cs`

### If you use XAMPP
1. Open **XAMPP Control Panel**.
2. Start **Apache** if you need phpMyAdmin, and start **MySQL**.
3. Make sure MySQL shows as running.
4. You do **not** need to manually create `topcuts_salon_inventory` for a fresh installation. The application creates it automatically.
5. Run the application from Visual Studio.

### If you use WAMP
1. Start WAMP and make sure the MySQL service is running.
2. The default WAMP MySQL setup normally works with the same settings above.
3. Run the application from Visual Studio.

### If your MySQL uses a different port or password
Open:

`Database/DatabaseSettings.cs`

For example:

```csharp
public const string Server = "127.0.0.1";
public const uint Port = 3307;
public const string Username = "root";
public const string Password = "your_password";
public const string DatabaseName = "topcuts_salon_inventory";
```

Use your actual MySQL port and password.

## New features
- **Cost price & profit:** each product has a cost price; every sale stores the unit cost, and the dashboard shows today's profit. (Sales made before this update have no cost recorded and count as full profit.)
- **Low-stock alerts:** dashboard banner, red LOW STOCK card, list of products at or below their alert level, and a popup when the dashboard opens.
- **Receipts:** after completing a sale you can preview and print a receipt.
- **Suppliers & purchase orders:** manage suppliers, create purchase orders, and RECEIVE them to add stock (recorded in the movement history and updating the product's cost price).

## Database creation
The application automatically:
1. Connects to the MySQL server.
2. Creates the `topcuts_salon_inventory` database if it does not exist; this separate database starts with no preloaded products.
3. Creates the required tables.
4. Upgrades the older product schema without deleting existing product data.
5. Does not create any default account; create your first staff account with REGISTER on the login screen.

You can also manually run `SQL/create_database.sql` in phpMyAdmin or MySQL Workbench if required by your class/project instructions.

## Running
1. Start MySQL in XAMPP, WAMP, or the MySQL Server service.
2. Open `CIT_Inventory_Management_System.slnx` or the `.csproj` in Visual Studio.
3. Restore NuGet packages.
4. Check `Database/DatabaseSettings.cs` if your MySQL credentials are different.
5. Build and run.
6. The application connects to MySQL and creates/updates the database automatically.
7. If MySQL is not running or the settings are wrong, the application stops before login and shows the connection settings to check.

## First login
There is no default account. On first run, click REGISTER on the login screen to create your staff account, then log in.
