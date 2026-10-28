using System.Security.Cryptography;
using System.Text;

namespace CIT_Inventory_Management_System;

public static class Security
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}
