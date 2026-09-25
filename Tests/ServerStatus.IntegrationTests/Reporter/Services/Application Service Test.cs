// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusCommon.Values;
using ServerStatusReporter.Abstractions;
using ServerStatusReporter.Services;
using System.Configuration;

namespace ServerStatus.IntegrationTests.Reporter.Services
{
    [TestClass]
    [DoNotParallelize]
    public class ApplicationServiceTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly Mock<IClock> _MockClock = new();
        private readonly Mock<ITCPClient> _MockTCPClient = new();
        private readonly Mock<IProcessService> _MockProcessService = new();

        /// <summary>
        /// Sets up the ConfigurationManager so the static AppSettingsModel can initialise.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ConfigurationManager.AppSettings["Servers"] = "TestServer";
            ConfigurationManager.AppSettings["Components"] = "PC,Server,Connection";
        }

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

        private PagedResponseModel<ServerModel> CreatePagedResponse(ServerModel server)
        {
            return new()
            {
                Entries = [server],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 10,
                TotalPageCount = 1,
                TotalCount = 1
            };
        }

        private EventModel CreateEventModel()
        {
            return new()
            {
                Id = 1,
                Component = StandardValues.ComponentValues.PC,
                Status = StandardValues.StatusValues.Online,
                DateOccured = DateTime.UtcNow,
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };
        }

        /// <summary>
        /// Checks whether the Setup method configures the timer without throwing.
        /// </summary>
        [TestMethod]
        public void TestSetup()
        {
            SharedSettingsModel sharedSettings = new();

            Mock<IAPIClient> _mockAPIClient = new();
            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockLogger.Verify(
                l => l.LogMessage("Info", It.Is<string>(s => s.Contains("Configured Application Service"))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a PC Online event for a known server.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersPCEvent()
        {
            SharedSettingsModel sharedSettings = new();

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", StandardValues.ComponentValues.PC);
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.PC];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(
                    e => e.Component == StandardValues.ComponentValues.PC && e.Status == StandardValues.StatusValues.Online)),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Server Online event when the server process is running.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersServerOnlineEvent()
        {
            SharedSettingsModel sharedSettings = new();

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", StandardValues.ComponentValues.Server);
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.Server];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync("1234\r\n2026-09-01T10:00:00.0000000Z");

            _MockProcessService.Setup(ps => ps.IsRunning(1234, It.IsAny<DateTime>())).Returns(true);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(
                    e => e.Component == StandardValues.ComponentValues.Server && e.Status == StandardValues.StatusValues.Online)),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Connection Online event when the ping succeeds.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersConnectionOnlineEvent()
        {
            SharedSettingsModel sharedSettings = new();

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", StandardValues.ComponentValues.Connection);
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.Connection];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            _MockTCPClient.Setup(tcp => tcp.PingAddress("127.0.0.1", 25565))
                .ReturnsAsync(true);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(
                    e => e.Component == StandardValues.ComponentValues.Connection && e.Status == StandardValues.StatusValues.Online)),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Connection Offline event when the ping fails.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersConnectionOfflineEvent()
        {
            SharedSettingsModel sharedSettings = new();

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", StandardValues.ComponentValues.Connection);
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.Connection];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            _MockTCPClient.Setup(tcp => tcp.PingAddress("127.0.0.1", 25565))
                .ReturnsAsync(false);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(
                    e => e.Component == StandardValues.ComponentValues.Connection && e.Status == StandardValues.StatusValues.Offline)),
                Times.Once);
        }
        /// <summary>
        /// Checks whether the Run method registers a Server Offline event when the server process is not running.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersServerOfflineEvent()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.Server];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(c => c.RegisterServerEvent(
                It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(e => e.Component == StandardValues.ComponentValues.Server && e.Status == StandardValues.StatusValues.Offline)),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method skips event registration when the server is not found in the API.
        /// </summary>
        [TestMethod]
        public async Task TestRunServerNotFoundInAPI()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["UnknownServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.PC];

            PagedResponseModel<ServerModel> emptyResponse = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 10,
                TotalPageCount = 1,
                TotalCount = 0
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));

            await _applicationService.Start();

            _mockAPIClient.Verify(c => c.RegisterServerEvent(
                It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the Run method skips event registration when a recent event with the same status exists.
        /// </summary>
        [TestMethod]
        public async Task TestRunSkipsEventWhenRecentSameStatusExists()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.PC];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);

            DateTime testTime = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);

            EventModel recentEvent = new()
            {
                Id = 1,
                Component = StandardValues.ComponentValues.PC,
                Status = StandardValues.StatusValues.Online,
                DateOccured = testTime.AddSeconds(-2),
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel> { recentEvent }, true));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(testTime);

            await _applicationService.Start();

            _mockAPIClient.Verify(c => c.RegisterServerEvent(
                It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the Run method skips registration when a recent event with a different status exists.
        /// </summary>
        [TestMethod]
        public async Task TestRunSkipsRegistrationWhenRecentDifferentStatusExists()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.Server];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            DateTime testTime = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);

            EventModel recentEvent = new()
            {
                Id = 1,
                Component = StandardValues.ComponentValues.Server,
                Status = StandardValues.StatusValues.Online,
                DateOccured = testTime.AddSeconds(-2),
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel> { recentEvent }, true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(testTime);

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(
                    It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the Run method registers an event when the existing event is stale.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersEventWhenExistingEventIsStale()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.PC];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            DateTime testTime = new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);

            EventModel staleEvent = new()
            {
                Id = 1,
                Component = StandardValues.ComponentValues.PC,
                Status = StandardValues.StatusValues.Online,
                DateOccured = testTime.AddSeconds(-10),
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel> { staleEvent }, true));
            _mockAPIClient.Setup(c => c.RegisterServerEvent(It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()))
                .ReturnsAsync((createdEvent, (ResponseModel?)null));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(testTime);

            await _applicationService.Start();

            _mockAPIClient.Verify(
                c => c.RegisterServerEvent(It.Is<ServerStatusCommon.Models.Requests.Create.EventRequestModel>(
                    e => e.Component == StandardValues.ComponentValues.PC && e.Status == StandardValues.StatusValues.Online)),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method skips event registration when the server is in its downtime window.
        /// </summary>
        [TestMethod]
        public async Task TestRunSkipsRegistrationDuringDowntime()
        {
            SharedSettingsModel sharedSettings = new();

            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = [StandardValues.ComponentValues.PC];

            DateTime testTime = new(2026, 09, 01, 03, 0, 0, DateTimeKind.Utc);

            ServerModel server = CreateTestServer();
            server.Downtime = new()
            {
                Time = "03:00:00",
                Duration = 300
            };

            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
            _mockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((new List<EventModel>(), true));

            RetryService _retryService = new(_MockLogger.Object);
            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object,
                _retryService);

            Mock<IFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _MockLogger.Object,
                _mockFileSystem.Object);

            ApplicationService _applicationService = new(
                _MockLogger.Object,
                _MockClock.Object,
                _MockTCPClient.Object,
                _MockProcessService.Object,
                _apiService,
                _pidFileService,
                sharedSettings);

            _applicationService.Setup();

            _MockClock.Setup(c => c.UtcNow).Returns(testTime);

            await _applicationService.Start();

            _mockAPIClient.Verify(c => c.RegisterServerEvent(
                It.IsAny<ServerStatusCommon.Models.Requests.Create.EventRequestModel>()),
                Times.Never);
        }
    }
}
