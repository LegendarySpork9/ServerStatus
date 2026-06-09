// Copyright © - 05/10/2025 - Toby Hunter
using System.Security.Cryptography;
using System.Text;

namespace ServerStatusSite.Functions
{
    public static class HashFunction
    {
        /// <summary>
        /// Converts the given string to its hashed value.
        /// </summary>
        public static string HashString(string value)
        {
            string hashString = string.Empty;

            if (!string.IsNullOrWhiteSpace(value))
            {
                StringBuilder hashedValue = new();

                byte[] hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(value));

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashedValue.Append(hashBytes[i].ToString("x2"));
                }

                hashString = hashedValue.ToString();
            }

            return hashString;
        }
    }
}
