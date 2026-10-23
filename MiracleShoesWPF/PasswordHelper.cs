using System;
using System.Security.Cryptography;

namespace MiracleShoesWPF
{
    // Хеширование паролей: PBKDF2 (Rfc2898DeriveBytes) со случайной солью.
    // В базе хранится строка вида: итерации.соль.хеш (соль и хеш в Base64).
    internal static class PasswordHelper
    {
        private const int Iterations = 10000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                hash = pbkdf2.GetBytes(HashSize);
            }

            return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            string[] parts = stored.Split('.');
            if (parts.Length != 3) return false;

            int iterations;
            if (!int.TryParse(parts[0], out iterations)) return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                actual = pbkdf2.GetBytes(expected.Length);
            }

            // Сравнение за постоянное время
            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                diff |= actual[i] ^ expected[i];
            }
            return diff == 0;
        }
    }
}
