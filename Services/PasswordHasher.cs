using System.Security.Cryptography;

namespace CapstoneProjectPUSH_API.Services
{
    // PBKDF2 password hashing
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        // Hashes input password
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        // Verifies input password with stored password hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split('.');

            if (parts.Length != 3)
            {
                return false;
            }

            int iterations;

            if (!int.TryParse(parts[0], out iterations))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}