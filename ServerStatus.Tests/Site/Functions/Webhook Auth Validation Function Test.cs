// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Functions;
using System.Security.Cryptography;
using System.Text;

namespace ServerStatus.Tests.Site.Functions
{
    [TestClass]
    public class WebhookAuthValidationFunctionTest
    {
        private const string Secret = "test-secret";

        private static string ComputeSignature(
            string body,
            string secret)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using HMACSHA256 hmac = new(key);
            byte[] hash = hmac.ComputeHash(bodyBytes);

            return Convert.ToHexString(hash).ToLower();
        }

        [TestMethod]
        public void TestValidSignature_ReturnsTrue()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[]}";
            string signature = ComputeSignature(body, Secret);

            bool result = WebhookAuthValidationFunction.ValidateSignature(
                signature,
                body,
                Secret);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestInvalidSignature_ReturnsFalse()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[]}";

            bool result = WebhookAuthValidationFunction.ValidateSignature(
                "invalid-hex-signature",
                body,
                Secret);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestWrongSecret_ReturnsFalse()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[]}";
            string signature = ComputeSignature(body, "wrong-secret");

            bool result = WebhookAuthValidationFunction.ValidateSignature(
                signature,
                body,
                Secret);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestNullSignature_ReturnsFalse()
        {
            bool result = WebhookAuthValidationFunction.ValidateSignature(
                null,
                "body",
                Secret);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestEmptySignature_ReturnsFalse()
        {
            bool result = WebhookAuthValidationFunction.ValidateSignature(
                string.Empty,
                "body",
                Secret);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTamperedBody_ReturnsFalse()
        {
            string originalBody = "{\"serverName\":\"TestServer\",\"logs\":[]}";
            string signature = ComputeSignature(originalBody, Secret);

            bool result = WebhookAuthValidationFunction.ValidateSignature(
                signature,
                "{\"serverName\":\"HackedServer\",\"logs\":[]}",
                Secret);

            Assert.IsFalse(result);
        }
    }
}
