// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.AspNetCore.Components;
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

        /// <summary>
        /// Checks whether the Next Page button appears when there are multiple pages.
        /// </summary>
        [TestMethod]
        public void TestShowsNextPageButtonWhenMultiplePages()
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
                TotalPageCount = 3,
                TotalCount = 75
            };

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyServers, true));
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

            var buttons = cut.FindAll("button.btn-primary");
            bool hasNextPage = buttons.Any(b => b.TextContent.Contains("Next Page"));
            bool hasPreviousPage = buttons.Any(b => b.TextContent.Contains("Previous Page"));

            Assert.IsTrue(hasNextPage);
            Assert.IsFalse(hasPreviousPage);
        }

        /// <summary>
        /// Checks whether the server filter dropdown renders server names.
        /// </summary>
        [TestMethod]
        public void TestServerFilterDropdownRendersServerNames()
        {
            PagedResponseModel<ServerModel> pagedServers = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Name = "Server Alpha",
                        HostName = "alpha-host",
                        Game = "Minecraft",
                        GameVersion = "1.7.10",
                        Connection = new() { IPAddress = "127.0.0.1", Port = 25565 },
                        Downtime = null,
                        EventInterval = 5,
                        WebhookURL = "https://discord.com/webhook",
                        RecipientId = 123456789,
                        IsActive = true
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Server Beta",
                        HostName = "beta-host",
                        Game = "Minecraft",
                        GameVersion = "1.7.10",
                        Connection = new() { IPAddress = "127.0.0.2", Port = 25565 },
                        Downtime = null,
                        EventInterval = 5,
                        WebhookURL = "https://discord.com/webhook2",
                        RecipientId = 987654321,
                        IsActive = true
                    }
                ],
                EntryCount = 2,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 2
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
                .ReturnsAsync((pagedServers, true));
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

            AngleSharp.Dom.IElement select = cut.Find("select");
            var options = select.QuerySelectorAll("option");

            Assert.IsTrue(options.Any(o => o.TextContent.Contains("All Servers")));
            Assert.IsTrue(options.Any(o => o.TextContent.Contains("Server Alpha")));
            Assert.IsTrue(options.Any(o => o.TextContent.Contains("Server Beta")));
        }

        /// <summary>
        /// Checks whether an admin clicking on an alert navigates to the edit alert page.
        /// </summary>
        [TestMethod]
        public void TestOpenClickNavigatesToEditAlertForAdmin()
        {
            UserModel adminUser = new()
            {
                Id = 1,
                Username = "Admin",
                Password = "HashedPassword",
                Scopes = ["User"],
                Settings =
                [
                    new() { Id = 1, Name = "DarkMode", Value = "False" },
                    new() { Id = 2, Name = "IsAdmin", Value = "True" },
                    new() { Id = 3, Name = "DiscordName", Value = "Admin" }
                ]
            };

            PagedResponseModel<ServerModel> emptyServers = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 0
            };

            PagedResponseModel<AlertModel> pagedAlerts = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 42,
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
                .ReturnsAsync((emptyServers, true));
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
            _Context.Services.AddSingleton(adminUser);

            IRenderedComponent<Alerts> cut = _Context.RenderComponent<Alerts>();

            AngleSharp.Dom.IElement alertRow = cut.Find("tr.clickable-row");
            alertRow.Click();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();

            Assert.IsTrue(nav.Uri.Contains("/editalert?alertId=42"));
        }
    }
}
