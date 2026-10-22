using System.Security.Cryptography;

namespace TaskManagement.Api.Services
{
    public static class PasswordHelper
    {
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations,
                HashAlgorithmName.SHA256, 32);

            return Convert.ToBase64String(salt) + "." +
                   Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, Iterations,
                    HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}