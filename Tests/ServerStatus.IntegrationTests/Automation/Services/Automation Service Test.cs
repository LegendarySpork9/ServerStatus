// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusAutomation.Services;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;

namespace ServerStatus.IntegrationTests.Automation.Services
{
    [TestClass]
    [DoNotParallelize]
    public class AutomationServiceTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly Mock<IClock> _MockClock = new();
        private readonly Mock<IHTTPClient> _MockHTTPClient = new();

        private readonly DateTime Expires = new(2026, 09, 01, 16, 00, 00, DateTimeKind.Utc);

        private ServerModel CreateTestServer()
        {
            return new()
            {
                Id = 1,
                Name = "TestServer",
                HostName = "test-host",
                Game = "TestGame",
                GameVersion = "1.0",
                Connection = new()
                {
                    IPAddress = "127.0.0.1",
                    Port = 25565
                },
                Downtime = null,
                EventInterval = 5,
                WebhookURL = "https://discord.com/webhook",
                RecipientId = 123456789,
                IsActive = true
            };
        }

        private RelatedServerModel CreateRelatedServer()
        {
            return new()
            {
                Id = 1,
                Name = "TestServer",
                HostName = "test-host",
                Game = "TestGame",
                GameVersion = "1.0"
            };
        }

        private SharedSettingsModel CreateSharedSettings()
        {
            return new()
            {
                Domain = "https://example.com",
                RecipientId = 123456789,
                SendAlerts = false,
                BaseURL = "https://api.example.com",
                Credentials = "Basic dGVzdDp0ZXN0",
                AuthPayloadLocation = "payload.json",
                RefreshTime = 5
            };
        }

        /// <summary>
        /// Checks whether the Setup method configures the timer without throwing.
        /// </summary>
        [TestMethod]
        public void TestSetup()
        {
            SharedSettingsModel sharedSettings = CreateSharedSettings();

            Mock<IAPIClient> _mockAPIClient = new();
            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();

            _MockLogger.Verify(
                l => l.LogMessage("Info", It.Is<string>(s => s.Contains("Configured Automation Service"))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method detects an outdated status and registers a new Unknown event.
        /// </summary>
        [TestMethod]
        public async Task TestRunDetectsOutdatedStatus()
        {
            DateTime utcNow = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(utcNow);

            SharedSettingsModel sharedSettings = CreateSharedSettings();
            ServerModel server = CreateTestServer();

            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components =
            [
                new() { Id = 1, Name = "PC" }
            ];

            EventModel outdatedEvent = new()
            {
                Id = 1,
                Component = "PC",
                Status = "Online",
                DateOccured = utcNow.AddMinutes(-10),
                Server = CreateRelatedServer()
            };

            EventModel createdEvent = new()
            {
                Id = 2,
                Component = "PC",
                Status = "Unknown",
                DateOccured = utcNow,
                Server = CreateRelatedServer()
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _mockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(([outdatedEvent], true));
            AlertModel createdAlert = new()
            {
                Id = 1,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Unknown",
                AlertStatus = "Reported",
                AlertDate = utcNow,
                Server = CreateRelatedServer()
            };

            _mockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(((PagedResponseModel<AlertModel>?)null, true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.RegisterAlert(It.IsAny<AlertRequestModel>()))
                .ReturnsAsync((createdAlert, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService)
            {
                ExpiryTime = Expires
            };

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();
            await _automationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<EventRequestModel>(
                    e => e.Component == "PC" && e.Status == "Unknown")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method does not trigger an alert for a recent Online status within the event interval.
        /// </summary>
        [TestMethod]
        public async Task TestRunOnlineStatusWithinInterval()
        {
            DateTime utcNow = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(utcNow);

            SharedSettingsModel sharedSettings = CreateSharedSettings();
            ServerModel server = CreateTestServer();

            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components =
            [
                new() { Id = 1, Name = "PC" }
            ];

            EventModel recentEvent = new()
            {
                Id = 1,
                Component = "PC",
                Status = "Online",
                DateOccured = utcNow.AddMinutes(-2),
                Server = CreateRelatedServer()
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _mockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(([recentEvent], true));
            _mockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(((PagedResponseModel<AlertModel>?)null, true));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService)
            {
                ExpiryTime = Expires
            };

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();
            await _automationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterAlert(It.IsAny<AlertRequestModel>()),
                Times.Never);
            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.IsAny<EventRequestModel>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the Run method creates an alert when the status is Offline and no existing alert is found.
        /// </summary>
        [TestMethod]
        public async Task TestRunCreatesAlertForOfflineStatus()
        {
            DateTime utcNow = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(utcNow);

            SharedSettingsModel sharedSettings = CreateSharedSettings();
            ServerModel server = CreateTestServer();

            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components =
            [
                new() { Id = 1, Name = "PC" }
            ];

            EventModel offlineEvent = new()
            {
                Id = 1,
                Component = "PC",
                Status = "Offline",
                DateOccured = utcNow.AddMinutes(-2),
                Server = CreateRelatedServer()
            };

            PagedResponseModel<AlertModel> emptyAlerts = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 0,
                TotalCount = 0
            };

            AlertModel createdAlert = new()
            {
                Id = 1,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = utcNow,
                Server = CreateRelatedServer()
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _mockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(([offlineEvent], true));
            _mockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyAlerts, true));
            _mockAPIClient.Setup(c => c.RegisterAlert(It.IsAny<AlertRequestModel>()))
                .ReturnsAsync((createdAlert, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService)
            {
                ExpiryTime = Expires
            };

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();
            await _automationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterAlert(It.Is<AlertRequestModel>(
                    a => a.Component == "PC" && a.ComponentStatus == "Offline" && a.Reporter == "Automation")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method skips creating an alert when an unresolved alert already exists.
        /// </summary>
        [TestMethod]
        public async Task TestRunSkipsAlertForExistingUnresolvedAlert()
        {
            DateTime utcNow = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(utcNow);

            SharedSettingsModel sharedSettings = CreateSharedSettings();
            ServerModel server = CreateTestServer();

            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components =
            [
                new() { Id = 1, Name = "PC" }
            ];

            EventModel offlineEvent = new()
            {
                Id = 1,
                Component = "PC",
                Status = "Offline",
                DateOccured = utcNow.AddMinutes(-2),
                Server = CreateRelatedServer()
            };

            AlertModel existingAlert = new()
            {
                Id = 1,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = utcNow.AddMinutes(-10),
                Server = CreateRelatedServer()
            };

            PagedResponseModel<AlertModel> alerts = new()
            {
                Entries = [existingAlert],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _mockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(([offlineEvent], true));
            _mockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((alerts, true));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService)
            {
                ExpiryTime = Expires
            };

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();
            await _automationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterAlert(It.IsAny<AlertRequestModel>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the Run method creates a new alert when the existing alert has been resolved.
        /// </summary>
        [TestMethod]
        public async Task TestRunCreatesAlertForResolvedAlert()
        {
            DateTime utcNow = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(utcNow);

            SharedSettingsModel sharedSettings = CreateSharedSettings();
            ServerModel server = CreateTestServer();

            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components =
            [
                new() { Id = 1, Name = "PC" }
            ];

            EventModel offlineEvent = new()
            {
                Id = 1,
                Component = "PC",
                Status = "Offline",
                DateOccured = utcNow.AddMinutes(-2),
                Server = CreateRelatedServer()
            };

            AlertModel resolvedAlert = new()
            {
                Id = 1,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Resolved",
                AlertDate = utcNow.AddMinutes(-30),
                Server = CreateRelatedServer()
            };

            PagedResponseModel<AlertModel> alerts = new()
            {
                Entries = [resolvedAlert],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };

            AlertModel createdAlert = new()
            {
                Id = 2,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = utcNow,
                Server = CreateRelatedServer()
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _mockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync(([offlineEvent], true));
            _mockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((alerts, true));
            _mockAPIClient.Setup(c => c.RegisterAlert(It.IsAny<AlertRequestModel>()))
                .ReturnsAsync((createdAlert, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService)
            {
                ExpiryTime = Expires
            };

            AutomationService _automationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockHTTPClient.Object,
                _apiService,
                sharedSettings);

            _automationService.Setup();
            await _automationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterAlert(It.Is<AlertRequestModel>(
                    a => a.Component == "PC" && a.ComponentStatus == "Offline" && a.Reporter == "Automation")),
                Times.Once);
        }
    }
}
