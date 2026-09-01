// Copyright © - 05/10/2025 - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Services;

namespace ServerStatus.IntegrationTests.Common.Services
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
                RecipientId = 123456789
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
                "This is a webhook",
                sharedSettings.RecipientId,
                "This is a message from a unit test.");

            Assert.IsTrue(successfulSend);
        }
    }
}
