// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Models;
using ServerStatusSite.Models.Responses.Related;
using ServerStatusSite.Services;
using ServerStatusSite.Webhooks;
using System.Security.Cryptography;
using System.Text;

namespace ServerStatus.IntegrationTests.Site.Webhooks
{
    [TestClass]
    public class LogWebhookControllerTest
    {
        private const string WebhookSecret = "test-secret";

        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly Mock<IBackupToolAPIClient> _MockBackupToolClient = new();

        private static string ComputeSignature(
            string body,
            string secret)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using HMACSHA256 hmac = new(key);
            byte[] hash = hmac.ComputeHash(bodyBytes);

            return Convert.ToHexString(hash)
                .ToLower();
        }

        private LogWebhookController CreateController(
            ILoggerService logger,
            LogStreamService logStream,
            BackupToolSettingsModel settings,
            string body,
            string? signature)
        {
            RetryService retryService = new(_MockLogger.Object);
            BackupToolAPIService backupToolApi = new(
                logger,
                _MockBackupToolClient.Object,
                retryService);

            LogWebhookController controller = new(
                logger,
                logStream,
                backupToolApi,
                settings);

            DefaultHttpContext httpContext = new();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

            if (signature != null)
            {
                httpContext.Request.Headers["X-Webhook-Secret"] = signature;
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        /// <summary>
        /// Checks whether the ReceiveLogs method returns OK for a valid signature and payload.
        /// </summary>
        [TestMethod]
        public async Task TestReceiveLogs_ReturnsOk_WhenValid()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[{\"id\":1,\"timestamp\":\"2026-08-28T12:00:00Z\",\"level\":\"Info\",\"type\":\"Tool\",\"message\":\"Test\"}]}";
            string signature = ComputeSignature(body, WebhookSecret);

            LogStreamService logStream = new();
            List<LogEntryModel>? received = null;

            logStream.Subscribe(
                "TestServer",
                logs =>
                {
                    received = logs;
                    return Task.CompletedTask;
                });

            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = WebhookSecret,
                SiteBaseURL = "https://example.com"
            };

            LogWebhookController controller = CreateController(
                _MockLogger.Object,
                logStream,
                settings,
                body,
                signature);

            IActionResult result = await controller.ReceiveLogs();

            Assert.IsInstanceOfType(
                result,
                typeof(OkResult));
            Assert.IsNotNull(received);
            Assert.AreEqual(
                1,
                received.Count);
        }

        /// <summary>
        /// Checks whether the ReceiveLogs method returns Unauthorized for an invalid signature.
        /// </summary>
        [TestMethod]
        public async Task TestReceiveLogs_ReturnsUnauthorized_WhenInvalidSignature()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[]}";

            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = WebhookSecret,
                SiteBaseURL = "https://example.com"
            };

            LogWebhookController controller = CreateController(
                _MockLogger.Object,
                new LogStreamService(),
                settings,
                body,
                "invalid-signature");

            IActionResult result = await controller.ReceiveLogs();

            Assert.IsInstanceOfType(
                result,
                typeof(UnauthorizedResult));
        }

        /// <summary>
        /// Checks whether the ReceiveLogs method returns Unauthorized when the signature is missing.
        /// </summary>
        [TestMethod]
        public async Task TestReceiveLogs_ReturnsUnauthorized_WhenNoSignature()
        {
            string body = "{\"serverName\":\"TestServer\",\"logs\":[]}";

            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = WebhookSecret,
                SiteBaseURL = "https://example.com"
            };

            LogWebhookController controller = CreateController(
                _MockLogger.Object,
                new LogStreamService(),
                settings,
                body,
                null);

            IActionResult result = await controller.ReceiveLogs();

            Assert.IsInstanceOfType(
                result,
                typeof(UnauthorizedResult));
        }

        /// <summary>
        /// Checks whether the ReceiveLogs method returns BadRequest for an invalid payload.
        /// </summary>
        [TestMethod]
        public async Task TestReceiveLogs_ReturnsBadRequest_WhenInvalidPayload()
        {
            string body = "{\"serverName\":\"\",\"logs\":null}";
            string signature = ComputeSignature(body, WebhookSecret);

            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = WebhookSecret,
                SiteBaseURL = "https://example.com"
            };

            LogWebhookController controller = CreateController(
                _MockLogger.Object,
                new LogStreamService(),
                settings,
                body,
                signature);

            IActionResult result = await controller.ReceiveLogs();

            Assert.IsInstanceOfType(
                result,
                typeof(BadRequestResult));
        }

        /// <summary>
        /// Checks whether the ReceiveLogs method returns 500 when the payload cannot be deserialised.
        /// </summary>
        [TestMethod]
        public async Task TestReceiveLogs_Returns500_WhenMalformedPayload()
        {
            string body = "not valid json at all";
            string signature = ComputeSignature(body, WebhookSecret);

            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = WebhookSecret,
                SiteBaseURL = "https://example.com"
            };

            LogWebhookController controller = CreateController(
                _MockLogger.Object,
                new LogStreamService(),
                settings,
                body,
                signature);

            IActionResult result = await controller.ReceiveLogs();
            StatusCodeResult statusCode = (StatusCodeResult)result;

            Assert.AreEqual(
                500,
                statusCode.StatusCode);
        }
    }
}
