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
        /// Checks whether the SendNotification method returns true when the message is sent successfully.
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

            bool actual = await _discordService.SendNotification(
                "This is a webhook",
                sharedSettings.RecipientId,
                "This is a message from a unit test.");

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Checks whether the SendNotification method returns false when the HTTP response indicates failure.
        /// </summary>
        [TestMethod]
        public async Task TestSendNotificationFailedResponse()
        {
            SharedSettingsModel sharedSettings = new()
            {
                SendAlerts = true,
                RecipientId = 123456789
            };

            HttpResponseMessage response = new()
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IHTTPClient> _mockHTTPClient = new();
            _mockHTTPClient.Setup(http => http.Send(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(response);

            DiscordService _discordService = new(
                _mockLogger.Object,
                _mockHTTPClient.Object,
                sharedSettings);

            bool actual = await _discordService.SendNotification(
                "This is a webhook",
                sharedSettings.RecipientId,
                "This message should fail.");

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Checks whether the SendNotification method returns false when the HTTP client returns null.
        /// </summary>
        [TestMethod]
        public async Task TestSendNotificationNullResponse()
        {
            SharedSettingsModel sharedSettings = new()
            {
                SendAlerts = true,
                RecipientId = 123456789
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IHTTPClient> _mockHTTPClient = new();
            _mockHTTPClient.Setup(http => http.Send(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync((HttpResponseMessage?)null);

            DiscordService _discordService = new(
                _mockLogger.Object,
                _mockHTTPClient.Object,
                sharedSettings);

            bool actual = await _discordService.SendNotification(
                "This is a webhook",
                sharedSettings.RecipientId,
                "This message returns null.");

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Checks whether the SendNotification method returns true without sending when SendAlerts is disabled.
        /// </summary>
        [TestMethod]
        public async Task TestSendNotificationAlertsDisabled()
        {
            SharedSettingsModel sharedSettings = new()
            {
                SendAlerts = false,
                RecipientId = 123456789
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IHTTPClient> _mockHTTPClient = new();

            DiscordService _discordService = new(
                _mockLogger.Object,
                _mockHTTPClient.Object,
                sharedSettings);

            bool actual = await _discordService.SendNotification(
                "This is a webhook",
                sharedSettings.RecipientId,
                "This should not send.");

            Assert.IsTrue(actual);
            _mockHTTPClient.Verify(
                http => http.Send(It.IsAny<HttpRequestMessage>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the SendNotification method returns false when the HTTP client throws an exception.
        /// </summary>
        [TestMethod]
        public async Task TestSendNotificationException()
        {
            SharedSettingsModel sharedSettings = new()
            {
                SendAlerts = true,
                RecipientId = 123456789
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IHTTPClient> _mockHTTPClient = new();
            _mockHTTPClient.Setup(http => http.Send(It.IsAny<HttpRequestMessage>()))
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            DiscordService _discordService = new(
                _mockLogger.Object,
                _mockHTTPClient.Object,
                sharedSettings);

            bool actual = await _discordService.SendNotification(
                "This is a webhook",
                sharedSettings.RecipientId,
                "This will throw.");

            Assert.IsFalse(actual);
        }
    }
}
