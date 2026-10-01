// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Models.Requests;
using ServerStatusSite.Models.Responses;
using ServerStatusSite.Services;

namespace ServerStatus.IntegrationTests.Site.Services
{
    [TestClass]
    public class BackupToolAPIServiceTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly RetryService _RetryService;

        public BackupToolAPIServiceTest()
        {
            _RetryService = new RetryService(_MockLogger.Object);
        }

        /// <summary>
        /// Checks whether the SendCommand method returns true when the command is sent successfully.
        /// </summary>
        [TestMethod]
        public async Task TestSendCommand_ReturnsTrue_WhenSuccessful()
        {
            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "stop"
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.SendCommand(
                    "TestServer",
                    It.Is<CommandRequestModel>(m => m.Target == "Server" && m.Command == "stop")))
                .ReturnsAsync((true, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.SendCommand(
                "TestServer",
                command);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks whether the SendCommand method returns false when the command fails.
        /// </summary>
        [TestMethod]
        public async Task TestSendCommand_ReturnsFalse_WhenFailed()
        {
            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "stop"
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.SendCommand(
                    "TestServer",
                    It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((false, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.SendCommand(
                "TestServer",
                command);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks whether the GetLogs method returns the expected logs when successful.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogs_ReturnsLogs_WhenSuccessful()
        {
            LogsResponseModel expected = new()
            {
                ServerName = "TestServer",
                Logs =
                [
                    new()
                    {
                        Id = 1,
                        Timestamp = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc),
                        Level = "Info",
                        Type = "Tool",
                        Message = "Test message"
                    }
                ],
                NextAfter = 1
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogs(
                    "TestServer",
                    It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((expected, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogsResponseModel? result = await service.GetLogs("TestServer");

            Assert.IsNotNull(result);
            Assert.AreEqual(
                1,
                result.Logs.Count);
        }

        /// <summary>
        /// Checks whether the GetLogs method returns null when the request fails.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogs_ReturnsNull_WhenFailed()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogs(
                    "TestServer",
                    It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(((LogsResponseModel?)null, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogsResponseModel? result = await service.GetLogs("TestServer");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the GetLogArchives method returns the expected archives when successful.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogArchives_ReturnsArchives_WhenSuccessful()
        {
            LogArchivesResponseModel expected = new()
            {
                ServerName = "TestServer",
                Archives =
                [
                    new()
                    {
                        FileName = "2026-08-28.zip",
                        CreatedAt = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        SizeBytes = 1024
                    }
                ]
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogArchives("TestServer"))
                .ReturnsAsync((expected, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogArchivesResponseModel? result = await service.GetLogArchives("TestServer");

            Assert.IsNotNull(result);
            Assert.AreEqual(
                1,
                result.Archives.Count);
        }

        /// <summary>
        /// Checks whether the RegisterWebhook method returns the expected registration when successful.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterWebhook_ReturnsRegistration_WhenSuccessful()
        {
            WebhookRegistrationResponseModel expected = new()
            {
                Id = "webhook-id",
                ServerName = "TestServer"
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.RegisterWebhook(
                    "TestServer",
                    It.IsAny<string>()))
                .ReturnsAsync((expected, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            WebhookRegistrationResponseModel? result = await service.RegisterWebhook(
                "TestServer",
                "https://example.com/webhook");

            Assert.IsNotNull(result);
            Assert.AreEqual(
                "webhook-id",
                result.Id);
        }

        /// <summary>
        /// Checks whether the UnregisterWebhook method returns true when successful.
        /// </summary>
        [TestMethod]
        public async Task TestUnregisterWebhook_ReturnsTrue_WhenSuccessful()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.UnregisterWebhook(
                    "TestServer",
                    "webhook-id"))
                .ReturnsAsync((true, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.UnregisterWebhook(
                "TestServer",
                "webhook-id");

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks whether the UnregisterWebhook method returns false when the request fails.
        /// </summary>
        [TestMethod]
        public async Task TestUnregisterWebhook_ReturnsFalse_WhenFailed()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.UnregisterWebhook(
                    "TestServer",
                    "webhook-id"))
                .ReturnsAsync((false, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.UnregisterWebhook(
                "TestServer",
                "webhook-id");

            Assert.IsFalse(result);
        }
        /// <summary>
        /// Checks whether the GetLogArchives method returns null when the request fails.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogArchives_ReturnsNull_WhenFailed()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogArchives("TestServer"))
                .ReturnsAsync(((LogArchivesResponseModel?)null, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogArchivesResponseModel? result = await service.GetLogArchives("TestServer");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the GetArchivedLogs method returns the expected logs when successful.
        /// </summary>
        [TestMethod]
        public async Task TestGetArchivedLogs_ReturnsLogs_WhenSuccessful()
        {
            ArchivedLogsResponseModel expected = new()
            {
                ServerName = "TestServer",
                ArchiveName = "2026-08-28.zip",
                Logs =
                [
                    new()
                    {
                        FileName = "server.log",
                        Content =
                        [
                            new() { Id = 1, Timestamp = DateTime.UtcNow, Level = "Info", Type = "Tool", Message = "Line 1" },
                            new() { Id = 2, Timestamp = DateTime.UtcNow, Level = "Info", Type = "Tool", Message = "Line 2" }
                        ]
                    }
                ]
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetArchivedLogs(
                    "TestServer",
                    "2026-08-28.zip"))
                .ReturnsAsync((expected, true));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            ArchivedLogsResponseModel? result = await service.GetArchivedLogs(
                "TestServer",
                "2026-08-28.zip");

            Assert.IsNotNull(result);
            Assert.AreEqual(
                1,
                result.Logs.Count);
        }

        /// <summary>
        /// Checks whether the GetArchivedLogs method returns null when the request fails.
        /// </summary>
        [TestMethod]
        public async Task TestGetArchivedLogs_ReturnsNull_WhenFailed()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetArchivedLogs(
                    "TestServer",
                    "2026-08-28.zip"))
                .ReturnsAsync(((ArchivedLogsResponseModel?)null, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            ArchivedLogsResponseModel? result = await service.GetArchivedLogs(
                "TestServer",
                "2026-08-28.zip");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the RegisterWebhook method returns null when the request fails.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterWebhook_ReturnsNull_WhenFailed()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.RegisterWebhook(
                    "TestServer",
                    It.IsAny<string>()))
                .ReturnsAsync(((WebhookRegistrationResponseModel?)null, false));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            WebhookRegistrationResponseModel? result = await service.RegisterWebhook(
                "TestServer",
                "https://example.com/webhook");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the GetLogs method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogs_ReturnsNull_WhenException()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogs(
                    "TestServer",
                    It.IsAny<List<KeyValuePair<string, object>>>()))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogsResponseModel? result = await service.GetLogs("TestServer");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the GetLogArchives method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogArchives_ReturnsNull_WhenException()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetLogArchives("TestServer"))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            LogArchivesResponseModel? result = await service.GetLogArchives("TestServer");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the GetArchivedLogs method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestGetArchivedLogs_ReturnsNull_WhenException()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.GetArchivedLogs(
                    "TestServer",
                    "2026-08-28.zip"))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            ArchivedLogsResponseModel? result = await service.GetArchivedLogs(
                "TestServer",
                "2026-08-28.zip");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the RegisterWebhook method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterWebhook_ReturnsNull_WhenException()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.RegisterWebhook(
                    "TestServer",
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            WebhookRegistrationResponseModel? result = await service.RegisterWebhook(
                "TestServer",
                "https://example.com/webhook");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the UnregisterWebhook method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestUnregisterWebhook_ReturnsFalse_WhenException()
        {
            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.UnregisterWebhook(
                    "TestServer",
                    "webhook-id"))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.UnregisterWebhook(
                "TestServer",
                "webhook-id");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks whether the SendCommand method handles an exception.
        /// </summary>
        [TestMethod]
        public async Task TestSendCommand_ReturnsFalse_WhenException()
        {
            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "stop"
            };

            Mock<IBackupToolAPIClient> _mockClient = new();
            _mockClient.Setup(c => c.SendCommand(
                    "TestServer",
                    It.IsAny<CommandRequestModel>()))
                .ThrowsAsync(new Exception("Network error"));

            BackupToolAPIService service = new(
                _MockLogger.Object,
                _mockClient.Object,
                _RetryService);

            bool result = await service.SendCommand(
                "TestServer",
                command);

            Assert.IsFalse(result);
        }
    }
}
