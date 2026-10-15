// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusCommon.Values;
using ServerStatusSite.Components.Pages.Alerts;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class EditAlertTest
    {
        private Bunit.TestContext _Context = null!;

        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IAPIClient> _MockAPIClient = null!;
        private Mock<IClock> _MockClock = null!;
        private Mock<IHTTPClient> _MockHTTPClient = null!;

        [TestInitialize]
        public void Setup()
        {
            _Context = new Bunit.TestContext();

            _MockLogger = new Mock<ILoggerService>();
            _MockAPIClient = new Mock<IAPIClient>();
            _MockClock = new Mock<IClock>();
            _MockHTTPClient = new Mock<IHTTPClient>();

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
                    new() { Id = 2, Name = "IsAdmin", Value = "True" },
                    new() { Id = 3, Name = "DiscordName", Value = "Tester" }
                ]
            };
        }

        private void RegisterServices()
        {
            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            SharedSettingsModel sharedSettings = new()
            {
                RecipientId = 123456789,
                SendAlerts = true
            };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IHTTPClient>(_MockHTTPClient.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(sharedSettings);
            _Context.Services.AddSingleton(CreateUser());
        }

        private AlertModel CreateTestAlert()
        {
            return new()
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
            };
        }

        private PagedResponseModel<ServerModel> CreateServerPage()
        {
            return new()
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
        }

        /// <summary>
        /// Checks whether the page renders the alert details.
        /// </summary>
        [TestMethod]
        public void TestRendersAlertDetails()
        {
            AlertModel alert = CreateTestAlert();

            _MockAPIClient.Setup(c => c.GetAlert(It.IsAny<int>()))
                .ReturnsAsync((alert, true));
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));

            RegisterServices();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("/editalert?alertId=1");

            IRenderedComponent<EditAlert> cut = _Context.RenderComponent<EditAlert>();

            Assert.IsTrue(cut.Markup.Contains("Reporter:"));
            Assert.IsTrue(cut.Markup.Contains("Occured:"));
            Assert.IsTrue(cut.Markup.Contains("Server:"));
            Assert.IsTrue(cut.Markup.Contains("Component:"));
            Assert.IsTrue(cut.Markup.Contains("Component Status:"));
            Assert.IsTrue(cut.Markup.Contains("Alert Status:"));
            Assert.IsTrue(cut.Markup.Contains("Save"));
        }

        /// <summary>
        /// Checks whether the page shows a warning when the alert is not found.
        /// </summary>
        [TestMethod]
        public void TestRendersAlertNotFoundWarning()
        {
            _MockAPIClient.Setup(c => c.GetAlert(It.IsAny<int>()))
                .ReturnsAsync(((AlertModel?)null, true));
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));

            RegisterServices();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("/editalert?alertId=999");

            IRenderedComponent<EditAlert> cut = _Context.RenderComponent<EditAlert>();

            Assert.IsTrue(cut.Markup.Contains("Alert not found."));
        }

        /// <summary>
        /// Checks whether the alert status dropdown contains the expected options.
        /// </summary>
        [TestMethod]
        public void TestAlertStatusDropdownOptions()
        {
            AlertModel alert = CreateTestAlert();

            _MockAPIClient.Setup(c => c.GetAlert(It.IsAny<int>()))
                .ReturnsAsync((alert, true));
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));

            RegisterServices();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("/editalert?alertId=1");

            IRenderedComponent<EditAlert> cut = _Context.RenderComponent<EditAlert>();

            AngleSharp.Dom.IElement select = cut.Find("select");
            var options = select.QuerySelectorAll("option");

            Assert.AreEqual(3, options.Length);
            Assert.AreEqual("Reported", options[0].TextContent.Trim());
            Assert.AreEqual("Investigating", options[1].TextContent.Trim());
            Assert.AreEqual("Resolved", options[2].TextContent.Trim());
        }

        /// <summary>
        /// Checks whether the page shows the server name from the alert.
        /// </summary>
        [TestMethod]
        public void TestRendersAlertServerName()
        {
            AlertModel alert = CreateTestAlert();

            _MockAPIClient.Setup(c => c.GetAlert(It.IsAny<int>()))
                .ReturnsAsync((alert, true));
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));

            RegisterServices();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("/editalert?alertId=1");

            IRenderedComponent<EditAlert> cut = _Context.RenderComponent<EditAlert>();

            Assert.IsTrue(cut.Markup.Contains("TestServer"));
        }

        /// <summary>
        /// Checks whether the page shows the reporter name.
        /// </summary>
        [TestMethod]
        public void TestRendersReporterName()
        {
            AlertModel alert = CreateTestAlert();

            _MockAPIClient.Setup(c => c.GetAlert(It.IsAny<int>()))
                .ReturnsAsync((alert, true));
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));

            RegisterServices();

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("/editalert?alertId=1");

            IRenderedComponent<EditAlert> cut = _Context.RenderComponent<EditAlert>();

            Assert.IsTrue(cut.Markup.Contains("Tester"));
        }
    }
}
