// Copyright © - 05/10/2025 - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Services;

namespace ServerStatus.Tests.Common.Services
{
    [TestClass]
    public class DiscordServiceTest
    {
        /// <summary>
        /// Checks whether the SendNotification method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestSendNotification()
        {
            SharedSettingsModel sharedSettings = new()
            {
                SendAlerts = true,
                WebhookURL = "This is a webhook",
                RecipientId = "4f84bf84bf84b8f74bf7348fb4"
            };

            HttpResponseMessage response = new()
            {
                StatusCode = System.Net.HttpStatusCode.OK
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IHTTPClient> _mockHTTPClient = new();
            _mockHTTPClient.Setup(http => http.Send(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(response);

            DiscordService _discordService = new(
                _mockLogger.Object,
                _mockHTTPClient.Object,
                sharedSettings);

            bool successfulSend = await _discordService.SendNotification(
                sharedSettings.RecipientId,
                "This is a message from a unit test.");

            Assert.IsTrue(successfulSend);
        }
    }
}
