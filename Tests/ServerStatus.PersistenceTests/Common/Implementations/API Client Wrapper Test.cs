// Copyright © - 31/08/2026 - Toby Hunter
using Moq;
using Newtonsoft.Json;
using RestSharp;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
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
    }
}
