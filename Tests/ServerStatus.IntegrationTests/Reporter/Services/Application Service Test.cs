// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
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
                Component = "PC",
                Status = "Online",
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
            SharedSettingsModel sharedSettings = new()
            {
                RefreshTime = 5
            };

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
            SharedSettingsModel sharedSettings = new()
            {
                RefreshTime = 5
            };

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", "PC");
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = ["PC"];

            ServerModel server = CreateTestServer();
            PagedResponseModel<ServerModel> pagedResponse = CreatePagedResponse(server);
            EventModel createdEvent = CreateEventModel();

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));
            _mockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedResponse, true));
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
                    e => e.Component == "PC" && e.Status == "Online")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Server Online event when the server process is running.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersServerOnlineEvent()
        {
            SharedSettingsModel sharedSettings = new()
            {
                RefreshTime = 5
            };

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", "Server");
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = ["Server"];

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
                    e => e.Component == "Server" && e.Status == "Online")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Connection Online event when the ping succeeds.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersConnectionOnlineEvent()
        {
            SharedSettingsModel sharedSettings = new()
            {
                RefreshTime = 5
            };

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", "Connection");
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = ["Connection"];

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
                    e => e.Component == "Connection" && e.Status == "Online")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Run method registers a Connection Offline event when the ping fails.
        /// </summary>
        [TestMethod]
        public async Task TestRunRegistersConnectionOfflineEvent()
        {
            SharedSettingsModel sharedSettings = new()
            {
                RefreshTime = 5
            };

            ConfigurationManager.AppSettings.Set("Servers", "TestServer");
            ConfigurationManager.AppSettings.Set("Components", "Connection");
            ServerStatusReporter.Models.AppSettingsModel.Servers = ["TestServer"];
            ServerStatusReporter.Models.AppSettingsModel.Components = ["Connection"];

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
                    e => e.Component == "Connection" && e.Status == "Offline")),
                Times.Once);
        }
    }
}
