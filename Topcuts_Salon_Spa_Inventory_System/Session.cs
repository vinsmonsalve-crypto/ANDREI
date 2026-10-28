namespace CIT_Inventory_Management_System;

public static class Session
{
    public static int? UserId { get; set; }
    public static string FullName { get; set; } = "";
    public static string Username { get; set; } = "";

    public static void Clear()
    {
        UserId = null;
        FullName = "";
        Username = "";
    }
}
