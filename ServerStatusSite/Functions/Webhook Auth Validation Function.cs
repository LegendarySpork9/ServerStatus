// Copyright © - Unpublished - Toby Hunter
using System.Security.Cryptography;
using System.Text;

namespace ServerStatusSite.Functions
{
    public static class WebhookAuthValidationFunction
    {
        /// <summary>
        /// Validates the HMAC-SHA256 signature against the request body.
        /// </summary>
        public static bool ValidateSignature(
            string? signature,
            string body,
            string secret)
        {
            bool authenticated = false;

            if (!string.IsNullOrEmpty(signature))
            {
                byte[] key = Encoding.UTF8.GetBytes(secret);
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

                using (HMACSHA256 hmac = new(key))
                {
                    byte[] computed = hmac.ComputeHash(bodyBytes);
                    byte[] received;

                    try
                    {
                        received = Convert.FromHexString(signature);
                    }

                    catch
                    {
                        received = [];
                    }

                    authenticated = CryptographicOperations.FixedTimeEquals(
                        computed,
                        received);
                }
            }

            return authenticated;
        }
    }
}
