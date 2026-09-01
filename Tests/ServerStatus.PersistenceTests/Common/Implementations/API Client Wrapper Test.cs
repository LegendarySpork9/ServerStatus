// Copyright © - Unpublished - Toby Hunter
using Moq;
using Newtonsoft.Json;
using RestSharp;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using System.Net;

namespace ServerStatus.PersistenceTests.Common.Implementations
{
    [TestClass]
    public class APIClientWrapperTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();
        private readonly Mock<IFileSystem> _MockFileSystem = new();

        private SharedSettingsModel CreateSettings()
        {
            return new()
            {
                BaseURL = "https://api.example.com",
                Credentials = "Basic dGVzdDp0ZXN0",
                AuthPayloadLocation = "payload.json"
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
        /// Checks whether the Authorise method returns the authentication model on success.
        /// </summary>
        [TestMethod]
        public async Task TestAuthorise()
        {
            string expectedToken = "test-bearer-token";
            string responseJson = JsonConvert.SerializeObject(new AuthenticationModel
            {
                Type = "Bearer",
                Token = expectedToken,
                ExpiresIn = 3600,
                Info = new()
                {
                    ApplicationName = "Test",
                    Scopes = ["read"],
                    Issued = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddHours(1)
                }
            });

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            _MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync("{\"applicationName\":\"test\"}");

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            (AuthenticationModel? auth, ResponseModel? apiResponse) = await _wrapper.Authorise();

            Assert.IsNotNull(auth);
            Assert.AreEqual(
                expectedToken,
                auth.Token);
            Assert.IsNull(apiResponse);
        }

        /// <summary>
        /// Checks whether the Authorise method returns an error response on failure.
        /// </summary>
        [TestMethod]
        public async Task TestAuthoriseUnauthorised()
        {
            string responseJson = JsonConvert.SerializeObject(new { Error = "Invalid credentials" });

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.Unauthorized,
                responseJson);

            _MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync("{\"applicationName\":\"test\"}");

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            (AuthenticationModel? auth, ResponseModel? apiResponse) = await _wrapper.Authorise();

            Assert.IsNull(auth);
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                apiResponse.StatusCode);
        }

        /// <summary>
        /// Checks whether the GetServers method returns the server list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetServers()
        {
            PagedResponseModel<ServerModel> expected = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Name = "TestServer",
                        HostName = "test-host",
                        Game = "TestGame",
                        GameVersion = "1.0",
                        Connection = new() { IPAddress = "127.0.0.1", Port = 25565 },
                        Downtime = null,
                        EventInterval = 5,
                        WebhookURL = "https://discord.com/webhook",
                        RecipientId = 123456789,
                        IsActive = true
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 10,
                TotalPageCount = 1,
                TotalCount = 1
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (PagedResponseModel<ServerModel>? servers, bool success) = await _wrapper.GetServers([]);

            Assert.IsTrue(success);
            Assert.IsNotNull(servers);
            Assert.AreEqual(
                1,
                servers.EntryCount);
            Assert.AreEqual(
                "TestServer",
                servers.Entries[0].Name);
        }

        /// <summary>
        /// Checks whether the GetServers method returns success for NoContent.
        /// </summary>
        [TestMethod]
        public async Task TestGetServersNoContent()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.NoContent,
                null);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (PagedResponseModel<ServerModel>? servers, bool success) = await _wrapper.GetServers([]);

            Assert.IsTrue(success);
            Assert.IsNull(servers);
        }

        /// <summary>
        /// Checks whether the RegisterServerEvent method returns the created event on success.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterServerEvent()
        {
            EventModel expected = new()
            {
                Id = 42,
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

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.Created,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            EventRequestModel newEvent = new()
            {
                Component = "PC",
                Status = "Online",
                ServerId = 1,
                Name = "TestServer",
                HostName = "test-host",
                Game = "TestGame",
                GameVersion = "1.0"
            };

            (EventModel? createdEvent, ResponseModel? apiResponse) = await _wrapper.RegisterServerEvent(newEvent);

            Assert.IsNotNull(createdEvent);
            Assert.AreEqual(
                42,
                createdEvent.Id);
            Assert.IsNull(apiResponse);
        }

        /// <summary>
        /// Checks whether the GetComponents method returns the component list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetComponents()
        {
            var expected = new
            {
                Entries = new[]
                {
                    new { Name = "PC" },
                    new { Name = "Server" },
                    new { Name = "Connection" }
                }
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (List<ComponentModel> components, bool success) = await _wrapper.GetComponents();

            Assert.IsTrue(success);
            Assert.AreEqual(
                3,
                components.Count);
        }

        /// <summary>
        /// Checks whether the SetBearerToken method stores the token for subsequent requests.
        /// </summary>
        [TestMethod]
        public async Task TestSetBearerTokenUsedInRequests()
        {
            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.NoContent,
                null);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("my-token-123");

            await _wrapper.GetServers([]);

            _mockRestClient.Verify(
                rc => rc.ExecuteAsync(
                    It.IsAny<string>(),
                    It.Is<RestRequest>(r => r.Parameters.Any(
                        p => p.Name == "Authorization" && p.Value != null && p.Value.ToString()!.Contains("my-token-123")))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the GetUsers method returns the paged user list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetUsers()
        {
            PagedResponseModel<UserModel> expected = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Username = "TestUser",
                        Password = "HashedString",
                        Scopes = ["User"]
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (PagedResponseModel<UserModel>? users, bool success) = await _wrapper.GetUsers([]);

            Assert.IsTrue(success);
            Assert.IsNotNull(users);
            Assert.AreEqual(
                1,
                users.EntryCount);
            Assert.AreEqual(
                "TestUser",
                users.Entries[0].Username);
        }

        /// <summary>
        /// Checks whether the GetUserSettings method returns the settings list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetUserSettings()
        {
            List<UserSettingModel> expected =
            [
                new()
                {
                    Application = "Server Status Site",
                    Settings =
                    [
                        new() { Id = 1, Name = "DarkMode", Value = "True" },
                        new() { Id = 2, Name = "IsAdmin", Value = "False" }
                    ]
                }
            ];

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (List<UserSettingModel> userSettings, bool success) = await _wrapper.GetUserSettings(1);

            Assert.IsTrue(success);
            Assert.AreEqual(
                1,
                userSettings.Count);
            Assert.AreEqual(
                2,
                userSettings[0].Settings.Count);
        }

        /// <summary>
        /// Checks whether the GetServerEvents method returns the event list on success.
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
                },
                new()
                {
                    Id = 2,
                    Component = "PC",
                    Status = "Offline",
                    DateOccured = DateTime.UtcNow.AddMinutes(-5),
                    Server = new()
                    {
                        Id = 2,
                        Name = "TestServer2",
                        HostName = "test-host-2",
                        Game = "TestGame",
                        GameVersion = "1.0"
                    }
                }
            ];

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (List<EventModel> events, bool success) = await _wrapper.GetServerEvents([new("component", "PC")]);

            Assert.IsTrue(success);
            Assert.AreEqual(
                2,
                events.Count);
        }

        /// <summary>
        /// Checks whether the UpdateUser method returns the updated user on success.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateUser()
        {
            UserModel expected = new()
            {
                Id = 1,
                Username = "UpdatedUser",
                Password = "NewHash",
                Scopes = ["User"]
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            UserUpdateRequestModel request = new()
            {
                Password = "NewHash"
            };

            (UserModel? actual, ResponseModel? apiResponse) = await _wrapper.UpdateUser(
                1,
                request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                "UpdatedUser",
                actual.Username);
            Assert.IsNull(apiResponse);
        }

        /// <summary>
        /// Checks whether the UpdateUser method returns an error response on failure.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateUserError()
        {
            string responseJson = JsonConvert.SerializeObject(new { Error = "User not found" });

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.NotFound,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            UserUpdateRequestModel request = new()
            {
                Password = "NewHash"
            };

            (UserModel? actual, ResponseModel? apiResponse) = await _wrapper.UpdateUser(
                99,
                request);

            Assert.IsNull(actual);
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(
                HttpStatusCode.NotFound,
                apiResponse.StatusCode);
        }

        /// <summary>
        /// Checks whether the GetAlerts method returns the paged alert list on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetAlerts()
        {
            PagedResponseModel<AlertModel> expected = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Reporter = "UnitTester",
                        Component = "PC",
                        ComponentStatus = "Offline",
                        AlertStatus = "Reported",
                        AlertDate = DateTime.UtcNow,
                        Server = new()
                        {
                            Id = 1,
                            Name = "TestServer",
                            HostName = "test-host",
                            Game = "TestGame",
                            GameVersion = "1.0"
                        }
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (PagedResponseModel<AlertModel>? alerts, bool success) = await _wrapper.GetAlerts([new("pageNumber", 1)]);

            Assert.IsTrue(success);
            Assert.IsNotNull(alerts);
            Assert.AreEqual(
                1,
                alerts.EntryCount);
            Assert.AreEqual(
                "Reported",
                alerts.Entries[0].AlertStatus);
        }

        /// <summary>
        /// Checks whether the GetAlert method returns a single alert on success.
        /// </summary>
        [TestMethod]
        public async Task TestGetAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "UnitTester",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Investigating",
                AlertDate = DateTime.UtcNow,
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            (AlertModel? alert, bool success) = await _wrapper.GetAlert(1);

            Assert.IsTrue(success);
            Assert.IsNotNull(alert);
            Assert.AreEqual(
                1,
                alert.Id);
            Assert.AreEqual(
                "Investigating",
                alert.AlertStatus);
        }

        /// <summary>
        /// Checks whether the UpdateAlert method returns the updated alert on success.
        /// </summary>
        [TestMethod]
        public async Task TestUpdateAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "UnitTester",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Resolved",
                AlertDate = DateTime.UtcNow,
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.OK,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            AlertUpdateRequestModel request = new()
            {
                Status = "Resolved"
            };

            (AlertModel? actual, ResponseModel? apiResponse) = await _wrapper.UpdateAlert(
                1,
                request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                "Resolved",
                actual.AlertStatus);
            Assert.IsNull(apiResponse);
        }

        /// <summary>
        /// Checks whether the RegisterAlert method returns the created alert on success.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterAlert()
        {
            AlertModel expected = new()
            {
                Id = 1,
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = DateTime.UtcNow,
                Server = new()
                {
                    Id = 1,
                    Name = "TestServer",
                    HostName = "test-host",
                    Game = "TestGame",
                    GameVersion = "1.0"
                }
            };

            string responseJson = JsonConvert.SerializeObject(expected);

            Mock<IRestClientWrapper> _mockRestClient = CreateMockRestClient(
                HttpStatusCode.Created,
                responseJson);

            SharedSettingsModel settings = CreateSettings();
            APIClientWrapper _wrapper = new(
                _MockLogger.Object,
                _MockFileSystem.Object,
                _mockRestClient.Object,
                settings);

            _wrapper.SetBearerToken("test-token");

            AlertRequestModel request = new()
            {
                Reporter = "Automation",
                Component = "PC",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                ServerId = 1,
                Name = "TestServer",
                HostName = "test-host",
                Game = "TestGame",
                GameVersion = "1.0"
            };

            (AlertModel? actual, ResponseModel? apiResponse) = await _wrapper.RegisterAlert(request);

            Assert.IsNotNull(actual);
            Assert.AreEqual(
                1,
                actual.Id);
            Assert.AreEqual(
                "Reported",
                actual.AlertStatus);
            Assert.IsNull(apiResponse);
        }
    }
}
