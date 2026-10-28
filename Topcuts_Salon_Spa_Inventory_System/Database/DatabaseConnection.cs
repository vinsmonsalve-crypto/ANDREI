using MySql.Data.MySqlClient;

namespace CIT_Inventory_Management_System.Database;

public static class DatabaseConnection
{
    private static string ServerConnectionString =>
        $"server={DatabaseSettings.Server};port={DatabaseSettings.Port};uid={DatabaseSettings.Username};pwd={DatabaseSettings.Password};SslMode=None;AllowPublicKeyRetrieval=True;ConnectionTimeout=5;DefaultCommandTimeout=30;";

    public static string DatabaseName => DatabaseSettings.DatabaseName;

    public static string ConnectionString =>
        $"server={DatabaseSettings.Server};port={DatabaseSettings.Port};database={DatabaseName};uid={DatabaseSettings.Username};pwd={DatabaseSettings.Password};SslMode=None;AllowPublicKeyRetrieval=True;ConnectionTimeout=5;DefaultCommandTimeout=30;";

    public static MySqlConnection GetConnection() => new(ConnectionString);

    public static string GetConnectionSummary() =>
        $"Server: {DatabaseSettings.ServerDescription}\nDatabase: {DatabaseName}\nUser: {DatabaseSettings.Username}";

    public static void Initialize()
    {
        // First connect to MySQL itself. The application creates the database
        // automatically, so the user does not have to create topcuts_salon_inventory manually.
        using (var server = new MySqlConnection(ServerConnectionString))
        {
            server.Open();
            using var createDb = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                server);
            createDb.ExecuteNonQuery();
        }

        using var connection = GetConnection();
        connection.Open();

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                full_name VARCHAR(150) NOT NULL,
                username VARCHAR(80) NOT NULL UNIQUE,
                password_hash CHAR(64) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB;");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS products (
                id INT AUTO_INCREMENT PRIMARY KEY,
                product_id VARCHAR(30) NOT NULL UNIQUE,
                product_name VARCHAR(150) NOT NULL,
                category VARCHAR(80) NOT NULL,
                price DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                cost_price DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                quantity INT NOT NULL DEFAULT 0,
                low_stock_threshold INT NOT NULL DEFAULT 10,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX idx_products_name (product_name),
                INDEX idx_products_active (is_active)
            ) ENGINE=InnoDB;");

        // Upgrade the older project schema without deleting existing product data.
        MigrateLegacySchema(connection);

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS sales (
                id INT AUTO_INCREMENT PRIMARY KEY,
                invoice_no VARCHAR(40) NOT NULL UNIQUE,
                user_id INT NULL,
                total DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_sales_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
            ) ENGINE=InnoDB;");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS sale_items (
                id INT AUTO_INCREMENT PRIMARY KEY,
                sale_id INT NOT NULL,
                product_id INT NOT NULL,
                quantity INT NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL,
                total DECIMAL(12,2) NOT NULL,
                CONSTRAINT fk_sale_items_sale FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE,
                CONSTRAINT fk_sale_items_product FOREIGN KEY (product_id) REFERENCES products(id),
                INDEX idx_sale_items_sale (sale_id)
            ) ENGINE=InnoDB;");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS inventory_movements (
                id INT AUTO_INCREMENT PRIMARY KEY,
                product_id INT NOT NULL,
                movement_type VARCHAR(20) NOT NULL,
                quantity INT NOT NULL,
                reference_type VARCHAR(30) NULL,
                reference_id INT NULL,
                notes VARCHAR(255) NULL,
                user_id INT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_inventory_product FOREIGN KEY (product_id) REFERENCES products(id),
                CONSTRAINT fk_inventory_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
                INDEX idx_inventory_product_date (product_id, created_at),
                INDEX idx_inventory_type_date (movement_type, created_at)
            ) ENGINE=InnoDB;");

        // Cost of each unit at the time of sale, so profit stays correct even if the cost changes later.
        AddColumnIfMissing(connection, "sale_items", "unit_cost", "DECIMAL(10,2) NOT NULL DEFAULT 0.00");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS suppliers (
                id INT AUTO_INCREMENT PRIMARY KEY,
                supplier_name VARCHAR(150) NOT NULL,
                contact_person VARCHAR(120) NULL,
                phone VARCHAR(40) NULL,
                email VARCHAR(120) NULL,
                address VARCHAR(255) NULL,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_suppliers_name (supplier_name)
            ) ENGINE=InnoDB;");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS purchase_orders (
                id INT AUTO_INCREMENT PRIMARY KEY,
                po_number VARCHAR(40) NOT NULL UNIQUE,
                supplier_id INT NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
                notes VARCHAR(255) NULL,
                created_by INT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                received_at TIMESTAMP NULL DEFAULT NULL,
                CONSTRAINT fk_po_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
                CONSTRAINT fk_po_user FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL,
                INDEX idx_po_status (status)
            ) ENGINE=InnoDB;");

        Execute(connection, @"
            CREATE TABLE IF NOT EXISTS purchase_order_items (
                id INT AUTO_INCREMENT PRIMARY KEY,
                purchase_order_id INT NOT NULL,
                product_id INT NOT NULL,
                quantity INT NOT NULL,
                unit_cost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                CONSTRAINT fk_poi_po FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id) ON DELETE CASCADE,
                CONSTRAINT fk_poi_product FOREIGN KEY (product_id) REFERENCES products(id),
                INDEX idx_poi_po (purchase_order_id)
            ) ENGINE=InnoDB;");
    }

    private static void MigrateLegacySchema(MySqlConnection connection)
    {
        if (TableExists(connection, "products"))
        {
            AddColumnIfMissing(connection, "products", "cost_price", "DECIMAL(10,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(connection, "products", "low_stock_threshold", "INT NOT NULL DEFAULT 10");
            AddColumnIfMissing(connection, "products", "is_active", "TINYINT(1) NOT NULL DEFAULT 1");
            AddColumnIfMissing(connection, "products", "created_at", "TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            AddColumnIfMissing(connection, "products", "updated_at", "TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        }

        // The original project used a simple sales table. Rename it so the new
        // normalized sales + sale_items structure can be created without losing the old records.
        if (TableExists(connection, "sales") && !ColumnExists(connection, "sales", "invoice_no"))
        {
            var legacyName = "sales_legacy_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            using var rename = new MySqlCommand($"RENAME TABLE `sales` TO `{legacyName}`;", connection);
            rename.ExecuteNonQuery();
        }
    }

    private static bool TableExists(MySqlConnection connection, string table)
    {
        using var cmd = new MySqlCommand(@"
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = @table;", connection);
        cmd.Parameters.AddWithValue("@table", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(MySqlConnection connection, string table, string column)
    {
        using var cmd = new MySqlCommand(@"
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column;", connection);
        cmd.Parameters.AddWithValue("@table", table);
        cmd.Parameters.AddWithValue("@column", column);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static void AddColumnIfMissing(MySqlConnection connection, string table, string column, string definition)
    {
        if (ColumnExists(connection, table, column)) return;
        using var cmd = new MySqlCommand($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};", connection);
        cmd.ExecuteNonQuery();
    }

    private static void Execute(MySqlConnection connection, string sql)
    {
        using var command = new MySqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
