// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Components.Pages;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class HomeTest
    {
        private Bunit.TestContext _Context = null!;

        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IAPIClient> _MockAPIClient = null!;
        private Mock<IClock> _MockClock = null!;

        [TestInitialize]
        public void Setup()
        {
            _Context = new Bunit.TestContext();

            _MockLogger = new Mock<ILoggerService>();
            _MockAPIClient = new Mock<IAPIClient>();
            _MockClock = new Mock<IClock>();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _Context.Dispose();
        }

        private UserModel CreateUser()
        {
            return new()
            {
                Id = 1,
                Username = "TestUser",
                Password = "HashedPassword",
                Scopes = ["User"],
                Settings =
                [
                    new() { Id = 1, Name = "DarkMode", Value = "False" },
                    new() { Id = 2, Name = "IsAdmin", Value = "False" },
                    new() { Id = 3, Name = "DiscordName", Value = "Tester" }
                ]
            };
        }

        /// <summary>
        /// Checks whether the page displays servers and their component statuses.
        /// </summary>
        [TestMethod]
        public void TestRendersServersWithStatuses()
        {
            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Name = "TestServer",
                        HostName = "test-host",
                        Game = "Minecraft",
                        GameVersion = "1.7.10",
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
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            List<ComponentModel> components = [new() { Id = 1, Name = "PC" }];

            List<EventModel> events =
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
                        Game = "Minecraft",
                        GameVersion = "1.7.10"
                    }
                }
            ];

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((components, true));
            _MockAPIClient.Setup(c => c.GetServerEvents(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((events, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new() { RefreshTime = 5 };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IClock>(_MockClock.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Home> cut = _Context.RenderComponent<Home>();

            Assert.IsTrue(cut.Markup.Contains("TestServer"));
            Assert.IsTrue(cut.Markup.Contains("Online"));
        }

        /// <summary>
        /// Checks whether the page shows a message when no servers are found.
        /// </summary>
        [TestMethod]
        public void TestRendersNoServersMessage()
        {
            PagedResponseModel<ServerModel> emptyServers = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 0
            };

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyServers, true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((new List<ComponentModel>(), true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new() { RefreshTime = 5 };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IClock>(_MockClock.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Home> cut = _Context.RenderComponent<Home>();

            Assert.IsTrue(cut.Markup.Contains("No active servers"));
        }
    }
}
