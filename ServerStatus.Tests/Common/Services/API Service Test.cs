// Copyright © - 05/10/2025 - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;

namespace ServerStatus.Tests.Common.Services
{
    [TestClass]
    public class APIServiceTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly Mock<IClock> _MockClock = new();

        private readonly DateTime Expires = new(2026, 03, 12, 16, 00, 00, DateTimeKind.Utc);

        /// <summary>
        /// Sets the mocks up for the tests.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            DateTime utcNow = new(2026, 03, 12, 16, 00, 00, DateTimeKind.Utc);

            _MockClock.Setup(c => c.UtcNow)
                .Returns(utcNow);
        }

        /// <summary>
        /// Checks whether the Authorise method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestAuthorise()
        {
            AuthenticationModel expected = new()
            {
                Type = "Bearer",
                Token = "This is a token",
                ExpiresIn = 900,
                Info = new()
                {
                    ApplicationName = "Server Status",
                    Scopes = ["Server Status API"],
                    Issued = new(2026, 03, 12, 15, 45, 00, DateTimeKind.Utc),
                    Expires = Expires
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.Authorise())
                .ReturnsAsync(expected);

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object);
            await _apiService.Authorise();

            Assert.AreEqual(
                Expires,
                _apiService.ExpiryTime);
        }

        /// <summary>
        /// Checks whether the GetUsers method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetUsers()
        {
            List<UserModel> expected =
            [
                new()
                {
                    Id = 1,
                    Username = "Test",
                    Password = "HashedString",
                    Scopes = ["User"]
                }
            ];

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetUsers())
                .ReturnsAsync((
                    expected,
                    true));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            List<UserModel> actual = await _apiService.GetUsers();

            Assert.AreEqual(
                1,
                actual.Count);
            Assert.AreEqual(
                expected[0].Id,
                actual[0].Id);
            Assert.AreEqual(
                expected[0].Username,
                actual[0].Username);
            Assert.AreEqual(
                expected[0].Password,
                actual[0].Password);
        }

        /// <summary>
        /// Checks whether the GetUserSettings method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetUserSettings()
        {
            List<SettingModel> expected =
            [
                new()
                {
                    Id = 1,
                    Name = "DarkMode",
                    Value = "True"
                },
                new()
                {
                    Id = 2,
                    Name = "IsAdmin",
                    Value = "False"
                },
                new()
                {
                    Id = 3,
                    Name = "DiscordName",
                    Value = "UnitTester"
                }
            ];
            List<UserSettingModel> userSettings =
            [
                new()
                {
                    Application = "Server Status Site",
                    Settings = expected
                }
            ];

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetUserSettings(It.IsAny<int>()))
                .ReturnsAsync((
                    userSettings,
                    true));

            UserModel user = new()
            {
                Id = 1,
                Username = "Test",
                Password = "HashedString",
                Scopes = ["User"]
            };

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            UserModel actual = await _apiService.GetUserSettings(user);

            Assert.AreEqual(
                3,
                actual.Settings.Count);

            for (int x = 0; x < expected.Count; x++)
            {
                Assert.AreEqual(
                    expected[x].Id,
                    actual.Settings[x].Id);
                Assert.AreEqual(
                    expected[x].Name,
                    actual.Settings[x].Name);
                Assert.AreEqual(
                    expected[x].Value,
                    actual.Settings[x].Value);
            }
        }

        /// <summary>
        /// Checks whether the GetServers method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetServers()
        {
            List<ServerModel> expected =
            [
                new()
                {
                    Id = 1,
                    Name = "LocalHost",
                    HostName = "LocalHost",
                    Game = "Minecraft",
                    GameVersion = "1.7.10",
                    Connection = new()
                    {
                        IPAddress = "127.0.0.1",
                        Port = 25565
                    },
                    Downtime = null,
                    EventInterval = 60,
                    IsActive = true
                }
            ];

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetServers())
                .ReturnsAsync((
                    expected,
                    true));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            List<ServerModel> actual = await _apiService.GetServers();

            Assert.AreEqual(
                expected[0].Id,
                actual[0].Id);
            Assert.AreEqual(
                expected[0].Name,
                actual[0].Name);
            Assert.AreEqual(
                expected[0].HostName,
                actual[0].HostName);
            Assert.AreEqual(
                expected[0].Game,
                actual[0].Game);
            Assert.AreEqual(
                expected[0].GameVersion,
                actual[0].GameVersion);
            Assert.AreEqual(
                expected[0].Connection.IPAddress,
                actual[0].Connection.IPAddress);
            Assert.AreEqual(
                expected[0].Connection.Port,
                actual[0].Connection.Port);
            Assert.AreEqual(
                expected[0].Downtime,
                actual[0].Downtime);
            Assert.AreEqual(
                expected[0].EventInterval,
                actual[0].EventInterval);
            Assert.AreEqual(
                expected[0].IsActive,
                actual[0].IsActive);
        }

        /// <summary>
        /// Checks whether the GetServerEvents method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetServerEvents()
        {
            List<EventModel> expected =
            [
                new()
                {
                    Id = 1,
                    Component = "PC",
                    Status = "Offline",
                    DateOccured = new(2025, 10, 03, 20, 02, 53, DateTimeKind.Utc),
                    Server = new()
                    {
                        Id = 1,
                        Name = "LocalHost",
                        HostName = "LocalHost",
                        Game = "Minecraft",
                        GameVersion = "1.7.10"
                    }
                }
            ];

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((
                    expected,
                    true));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            List<EventModel> actual = await _apiService.GetServerEvents("PC");

            Assert.AreEqual(
                expected[0].Id,
                actual[0].Id);
            Assert.AreEqual(
                expected[0].Component,
                actual[0].Component);
            Assert.AreEqual(
                expected[0].Status,
                actual[0].Status);
            Assert.AreEqual(
                expected[0].DateOccured,
                actual[0].DateOccured);
            Assert.AreEqual(
                expected[0].Server.Id,
                actual[0].Server.Id);
            Assert.AreEqual(
                expected[0].Server.Name,
                actual[0].Server.Name);
            Assert.AreEqual(
                expected[0].Server.HostName,
                actual[0].Server.HostName);
            Assert.AreEqual(
                expected[0].Server.Game,
                actual[0].Server.Game);
            Assert.AreEqual(
                expected[0].Server.GameVersion,
                actual[0].Server.GameVersion);
        }

        /// <summary>
        /// Checks whether the UpdateUserSettings method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateUserSettings()
        {
            SettingModel expected = new()
            {
                Id = 1,
                Name = "DarkMode",
                Value = "False"
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.UpdateUserSettings(
                    It.IsAny<int>(),
                    It.IsAny<UserSettingUpdateRequestModel>()))
                .ReturnsAsync((
                    expected,
                    (ResponseModel?)null));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            UserSettingUpdateRequestModel request = new()
            {
                Value = "False"
            };

            (SettingModel? actual, ResponseModel? _) = await _apiService.UpdateUserSettings(
                1,
                request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Value,
                actual.Value);
        }

        /// <summary>
        /// Checks whether the UpdateUser method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateUser()
        {
            UserModel expected = new()
            {
                Id = 1,
                Username = "Test",
                Password = "HashedString",
                Scopes = ["User"]
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.UpdateUser(
                    It.IsAny<int>(),
                    It.IsAny<UserUpdateRequestModel>()))
                .ReturnsAsync((
                    expected,
                    (ResponseModel?)null));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            UserUpdateRequestModel request = new()
            {
                Password = "HashedString"
            };

            (UserModel? actual, ResponseModel? _) = await _apiService.UpdateUser(
                1,
                request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Password,
                actual.Password);
        }

        /// <summary>
        /// Checks whether the GetAlerts method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetAlerts()
        {
            AlertInformationModel expected = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Reporter = "UnitTester",
                        Component = "component",
                        ComponentStatus = "Offline",
                        AlertStatus = "Reported",
                        AlertDate = new(2025, 06, 14, 15, 39, 21, DateTimeKind.Utc),
                        Server = new()
                        {
                            Id = 1,
                            Name = "LocalHost",
                            HostName = "LocalHost",
                            Game = "Minecraft",
                            GameVersion = "1.7.10"
                        }
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((
                    expected,
                    true));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            AlertInformationModel? actual = await _apiService.GetAlerts(1);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Entries[0].Id,
                actual.Entries[0].Id);
            Assert.AreEqual(
                expected.Entries[0].Reporter,
                actual.Entries[0].Reporter);
            Assert.AreEqual(
                expected.Entries[0].Component,
                actual.Entries[0].Component);
            Assert.AreEqual(
                expected.Entries[0].ComponentStatus,
                actual.Entries[0].ComponentStatus);
            Assert.AreEqual(
                expected.Entries[0].AlertStatus,
                actual.Entries[0].AlertStatus);
            Assert.AreEqual(
                expected.Entries[0].AlertDate,
                actual.Entries[0].AlertDate);
            Assert.AreEqual(
                expected.Entries[0].Server.Id,
                actual.Entries[0].Server.Id);
            Assert.AreEqual(
                expected.Entries[0].Server.Name,
                actual.Entries[0].Server.Name);
            Assert.AreEqual(
                expected.Entries[0].Server.HostName,
                actual.Entries[0].Server.HostName);
            Assert.AreEqual(
                expected.Entries[0].Server.Game,
                actual.Entries[0].Server.Game);
            Assert.AreEqual(
                expected.Entries[0].Server.GameVersion,
                actual.Entries[0].Server.GameVersion);
        }

        /// <summary>
        /// Checks whether the GetAlert method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestGetAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "UnitTester",
                Component = "component",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = new(2025, 06, 14, 15, 39, 21, DateTimeKind.Utc),
                Server = new()
                {
                    Id = 1,
                    Name = "LocalHost",
                    HostName = "LocalHost",
                    Game = "Minecraft",
                    GameVersion = "1.7.10"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.GetAlert(It.IsAny<int>()))
                .ReturnsAsync((
                    expected,
                    true));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            AlertModel? actual = await _apiService.GetAlert(1);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Id,
                actual.Id);
            Assert.AreEqual(
                expected.Reporter,
                actual.Reporter);
            Assert.AreEqual(
                expected.Component,
                actual.Component);
            Assert.AreEqual(
                expected.ComponentStatus,
                actual.ComponentStatus);
            Assert.AreEqual(
                expected.AlertStatus,
                actual.AlertStatus);
            Assert.AreEqual(
                expected.AlertDate,
                actual.AlertDate);
            Assert.AreEqual(
                expected.Server.Id,
                actual.Server.Id);
            Assert.AreEqual(
                expected.Server.Name,
                actual.Server.Name);
            Assert.AreEqual(
                expected.Server.HostName,
                actual.Server.HostName);
            Assert.AreEqual(
                expected.Server.Game,
                actual.Server.Game);
            Assert.AreEqual(
                expected.Server.GameVersion,
                actual.Server.GameVersion);
        }

        /// <summary>
        /// Checks whether the UpdateAlert method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "UnitTester",
                Component = "component",
                ComponentStatus = "Offline",
                AlertStatus = "Investigating",
                AlertDate = new(2025, 06, 14, 15, 39, 21, DateTimeKind.Utc),
                Server = new()
                {
                    Id = 1,
                    Name = "LocalHost",
                    HostName = "LocalHost",
                    Game = "Minecraft",
                    GameVersion = "1.7.10"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.UpdateAlert(
                    It.IsAny<int>(),
                    It.IsAny<AlertUpdateRequestModel>()))
                .ReturnsAsync((
                    expected,
                    (ResponseModel?)null));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            AlertUpdateRequestModel request = new()
            {
                Status = "Investigating"
            };

            (AlertModel? actual, ResponseModel? _) = await _apiService.UpdateAlert(
                1,
                request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.AlertStatus,
                actual.AlertStatus);
        }

        /// <summary>
        /// Checks whether the RegisterAlert method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "UnitTester",
                Component = "component",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = new(2025, 06, 14, 15, 39, 21, DateTimeKind.Utc),
                Server = new()
                {
                    Id = 1,
                    Name = "LocalHost",
                    HostName = "LocalHost",
                    Game = "Minecraft",
                    GameVersion = "1.7.10"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.RegisterAlert(It.IsAny<AlertRequestModel>()))
                .ReturnsAsync((
                    expected,
                    (ResponseModel?)null));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            AlertRequestModel request = new()
            {
                Reporter = "UnitTester",
                Component = "component",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                ServerId = 1,
                Name = "LocalHost",
                HostName = "LocalHost",
                Game = "Minecraft",
                GameVersion = "1.7.10"
            };

            (AlertModel? actual, ResponseModel? _) = await _apiService.RegisterAlert(request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Id,
                actual.Id);
            Assert.AreEqual(
                expected.Reporter,
                actual.Reporter);
            Assert.AreEqual(
                expected.Component,
                actual.Component);
            Assert.AreEqual(
                expected.ComponentStatus,
                actual.ComponentStatus);
            Assert.AreEqual(
                expected.AlertStatus,
                actual.AlertStatus);
            Assert.AreEqual(
                expected.AlertDate,
                actual.AlertDate);
            Assert.AreEqual(
                expected.Server.Id,
                actual.Server.Id);
            Assert.AreEqual(
                expected.Server.Name,
                actual.Server.Name);
            Assert.AreEqual(
                expected.Server.HostName,
                actual.Server.HostName);
            Assert.AreEqual(
                expected.Server.Game,
                actual.Server.Game);
            Assert.AreEqual(
                expected.Server.GameVersion,
                actual.Server.GameVersion);
        }

        /// <summary>
        /// Checks whether the RegisterServerEvent method works as expected.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterServerEvent()
        {
            EventModel expected = new()
            {
                Id = 1,
                Component = "component",
                Status = "Offline",
                DateOccured = new(2025, 06, 14, 15, 39, 21, DateTimeKind.Utc),
                Server = new()
                {
                    Id = 1,
                    Name = "LocalHost",
                    HostName = "LocalHost",
                    Game = "Minecraft",
                    GameVersion = "1.7.10"
                }
            };

            Mock<IAPIClient> _mockAPIClient = new();
            _mockAPIClient.Setup(api => api.RegisterServerEvent(It.IsAny<EventRequestModel>()))
                .ReturnsAsync((
                    expected,
                    (ResponseModel?)null));

            APIService _apiService = new(
                _MockLogger.Object,
                _mockAPIClient.Object,
                _MockClock.Object)
            {
                ExpiryTime = Expires
            };

            EventRequestModel request = new()
            {
                Component = "component",
                Status = "Offline",
                ServerId = 1,
                Name = "LocalHost",
                HostName = "LocalHost",
                Game = "Minecraft",
                GameVersion = "1.7.10"
            };

            (EventModel? actual, ResponseModel? _) = await _apiService.RegisterServerEvent(request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                expected.Id,
                actual.Id);
            Assert.AreEqual(
                expected.Component,
                actual.Component);
            Assert.AreEqual(
                expected.Status,
                actual.Status);
            Assert.AreEqual(
                expected.DateOccured,
                actual.DateOccured);
            Assert.AreEqual(
                expected.Server.Id,
                actual.Server.Id);
            Assert.AreEqual(
                expected.Server.Name,
                actual.Server.Name);
            Assert.AreEqual(
                expected.Server.HostName,
                actual.Server.HostName);
            Assert.AreEqual(
                expected.Server.Game,
                actual.Server.Game);
            Assert.AreEqual(
                expected.Server.GameVersion,
                actual.Server.GameVersion);
        }
    }
}
