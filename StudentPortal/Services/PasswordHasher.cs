using System.Security.Cryptography;
using System.Text;

namespace StudentPortal.Services
{
    public class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static bool PasswordVerify(string password, string? hashPassword)
        {
            string pass = HashPassword(password);
            return pass.Equals(hashPassword);
        }
    }
}
