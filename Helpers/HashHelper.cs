using System;

namespace MenuGoBE.Helpers
{
    public static class HashHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try 
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}
