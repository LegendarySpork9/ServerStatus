// Copyright © - 31/08/2026 - Toby Hunter
using Moq;
using Newtonsoft.Json;
using RestSharp;
using ServerStatusCommon.Abstractions;
using ServerStatusSite.Implementations;
using ServerStatusSite.Models;
using ServerStatusSite.Models.Requests;
using ServerStatusSite.Models.Responses;
using ServerStatusSite.Models.Responses.Related;
using System.Net;

namespace ServerStatus.PersistenceTests.Site.Implementations
{
    [TestClass]
    public class BackupToolAPIClientWrapperTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();

        private BackupToolSettingsModel CreateSettings()
        {
            return new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "test-secret",
                SiteBaseURL = "https://site.example.com",
                Servers = new()
                {
                    ["TestServer"] = new()
                    {
                        ClientId = "test-client",
                        ClientSecret = "test-secret"
                    }
                }
            };
        }

        private Mock<IRestClientWrapper> CreateMockRestClient(
            HttpStatusCode statusCode,
            string? content)
        {
            Mock<IRestClientWrapper> mock = new();
            RestResponse response = new()
            {
                StatusCode = statusCode,
                Content = content
            };
            mock.Setup(rc => rc.ExecuteAsync(It.IsAny<string>(), It.IsAny<RestRequest>()))
                .ReturnsAsync(response);

            return mock;
        }

        /// <summary>
        /// Checks whether the GetLogs method returns the log list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogs()
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
                        Message = "Test log"
                    }
                ],
                NextAfter = 1
            };

            string responseJson = JsonConvert.SerializeObject(expected);
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (LogsResponseModel? logs, bool success) = await _wrapper.GetLogs(
                "TestServer",
                []);

            Assert.IsTrue(success);
            Assert.IsNotNull(logs);
            Assert.AreEqual(
                1,
                logs.Logs.Count);
        }

        /// <summary>
        /// Checks whether the GetLogs method returns success for NoContent.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogsNoContent()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.NoContent,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (LogsResponseModel? logs, bool success) = await _wrapper.GetLogs(
                "TestServer",
                []);

            Assert.IsTrue(success);
            Assert.IsNull(logs);
        }

        /// <summary>
        /// Checks whether the GetLogs method returns failure for an unknown server.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogsUnknownServer()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (LogsResponseModel? logs, bool success) = await _wrapper.GetLogs(
                "UnknownServer",
                []);

            Assert.IsFalse(success);
            Assert.IsNull(logs);
            _mockRestClient.Verify(
                rc => rc.ExecuteAsync(It.IsAny<string>(), It.IsAny<RestRequest>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the GetLogArchives method returns the archive list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetLogArchives()
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

            string responseJson = JsonConvert.SerializeObject(expected);
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (LogArchivesResponseModel? archives, bool success) = await _wrapper.GetLogArchives("TestServer");

            Assert.IsTrue(success);
            Assert.IsNotNull(archives);
            Assert.AreEqual(
                1,
                archives.Archives.Count);
        }

        /// <summary>
        /// Checks whether the GetArchivedLogs method returns the archived log content on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetArchivedLogs()
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
                            new()
                            {
                                Id = 1,
                                Timestamp = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc),
                                Level = "Info",
                                Type = "Tool",
                                Message = "Archived log entry"
                            }
                        ]
                    }
                ]
            };

            string responseJson = JsonConvert.SerializeObject(expected);
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (ArchivedLogsResponseModel? archivedLogs, bool success) = await _wrapper.GetArchivedLogs(
                "TestServer",
                "2026-08-28.zip");

            Assert.IsTrue(success);
            Assert.IsNotNull(archivedLogs);
            Assert.AreEqual(
                1,
                archivedLogs.Logs.Count);
        }

        /// <summary>
        /// Checks whether the RegisterWebhook method returns the registration on success.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterWebhook()
        {
            WebhookRegistrationResponseModel expected = new()
            {
                Id = "webhook-123",
                ServerName = "TestServer"
            };

            string responseJson = JsonConvert.SerializeObject(expected);
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.Created,
                responseJson);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (WebhookRegistrationResponseModel? registration, bool success) = await _wrapper.RegisterWebhook(
                "TestServer",
                "{\"url\":\"https://example.com/webhook\"}");

            Assert.IsTrue(success);
            Assert.IsNotNull(registration);
            Assert.AreEqual(
                "webhook-123",
                registration.Id);
        }

        /// <summary>
        /// Checks whether the UnregisterWebhook method returns success on OK response.
        /// </summary>
        [TestMethod]
        public async Task TestUnregisterWebhook()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (bool success, bool _) = await _wrapper.UnregisterWebhook(
                "TestServer",
                "webhook-123");

            Assert.IsTrue(success);
        }

        /// <summary>
        /// Checks whether the UnregisterWebhook method returns failure on error response.
        /// </summary>
        [TestMethod]
        public async Task TestUnregisterWebhookNotFound()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.NotFound,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            (bool success, bool _) = await _wrapper.UnregisterWebhook(
                "TestServer",
                "nonexistent-webhook");

            Assert.IsFalse(success);
        }

        /// <summary>
        /// Checks whether the SendCommand method returns success on OK response.
        /// </summary>
        [TestMethod]
        public async Task TestSendCommand()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "stop"
            };

            (bool success, bool _) = await _wrapper.SendCommand(
                "TestServer",
                command);

            Assert.IsTrue(success);
        }

        /// <summary>
        /// Checks whether the SendCommand method returns failure on error response.
        /// </summary>
        [TestMethod]
        public async Task TestSendCommandFailed()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.BadRequest,
                null);

            BackupToolSettingsModel settings = CreateSettings();
            BackupToolAPIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _mockRestClient.Object,
                settings);

            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "invalid"
            };

            (bool success, bool _) = await _wrapper.SendCommand(
                "TestServer",
                command);

            Assert.IsFalse(success);
        }
    }
}
