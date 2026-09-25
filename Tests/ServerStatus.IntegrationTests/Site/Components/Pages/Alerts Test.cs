// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusCommon.Values;
using ServerStatusSite.Components.Pages.Alerts;
using ServerStatusSite.Models;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class AlertsTest
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
        /// Checks whether the page renders alerts in a table.
        /// </summary>
        [TestMethod]
        public void TestRendersAlerts()
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

            PagedResponseModel<AlertModel> pagedAlerts = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Reporter = "Tester",
                        Component = StandardValues.ComponentValues.PC,
                        ComponentStatus = StandardValues.StatusValues.Offline,
                        AlertStatus = "Reported",
                        AlertDate = new DateTime(2026, 09, 01, 10, 0, 0, DateTimeKind.Utc),
                        Server = new()
                        {
                            Id = 1,
                            Name = "TestServer",
                            HostName = "test-host",
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

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));
            _MockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedAlerts, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new();
            SiteSettingsModel siteSettings = new() { RefreshTime = 5 };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IClock>(_MockClock.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(siteSettings);
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Alerts> cut = _Context.RenderComponent<Alerts>();

            Assert.IsTrue(cut.Markup.Contains("Tester"));
            Assert.IsTrue(cut.Markup.Contains("Reported"));
            Assert.IsTrue(cut.Markup.Contains("TestServer"));
        }

        /// <summary>
        /// Checks whether the page shows a message when no alerts are registered.
        /// </summary>
        [TestMethod]
        public void TestRendersNoAlertsMessage()
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

            PagedResponseModel<AlertModel> emptyAlerts = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 0,
                TotalCount = 0
            };

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyServers, true));
            _MockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyAlerts, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new();
            SiteSettingsModel siteSettings = new() { RefreshTime = 5 };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IClock>(_MockClock.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(siteSettings);
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Alerts> cut = _Context.RenderComponent<Alerts>();

            Assert.IsTrue(cut.Markup.Contains("No alerts registered"));
        }

        /// <summary>
        /// Checks whether the page contains a Register Alert button.
        /// </summary>
        [TestMethod]
        public void TestRendersRegisterAlertButton()
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

            PagedResponseModel<AlertModel> emptyAlerts = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 0,
                TotalCount = 0
            };

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyServers, true));
            _MockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyAlerts, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new();
            SiteSettingsModel siteSettings = new() { RefreshTime = 5 };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IClock>(_MockClock.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(siteSettings);
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Alerts> cut = _Context.RenderComponent<Alerts>();

            AngleSharp.Dom.IElement registerButton = cut.Find("button.btn-primary");

            Assert.IsTrue(registerButton.TextContent.Contains("Register Alert"));
        }
    }
}
