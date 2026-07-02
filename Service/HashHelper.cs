using System;
using System.Security.Cryptography;
using System.Text;

namespace MenuGoBE.Service
{
    public static class HashHelper
    {
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
