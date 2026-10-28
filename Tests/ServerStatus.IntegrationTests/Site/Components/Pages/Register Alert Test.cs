// Copyright © - Unpublished - Toby Hunter
using Bunit;
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
    public class RegisterAlertTest
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
                    new() { Id = 2, Name = "IsAdmin", Value = "False" },
                    new() { Id = 3, Name = "DiscordName", Value = "Tester" }
                ]
            };
        }

        private void RegisterServices()
        {
            RetryService retryService = new(
                _MockLogger.Object,
                delaySeconds: 0,
                maxRetries: 4);
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
        /// Checks whether the page renders the form with dropdowns.
        /// </summary>
        [TestMethod]
        public void TestRendersFormWithDropdowns()
        {
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync(([new() { Id = 1, Name = StandardValues.ComponentValues.PC, IsDeleted = false }], true));

            RegisterServices();

            IRenderedComponent<RegisterAlert> cut = _Context.RenderComponent<RegisterAlert>();

            Assert.IsTrue(cut.Markup.Contains("Reporter:"));
            Assert.IsTrue(cut.Markup.Contains("Server:"));
            Assert.IsTrue(cut.Markup.Contains("Component:"));
            Assert.IsTrue(cut.Markup.Contains("Component Status:"));
            Assert.IsTrue(cut.Markup.Contains("Register"));
        }

        /// <summary>
        /// Checks whether the page shows no server options when the server list is empty.
        /// </summary>
        [TestMethod]
        public void TestRendersEmptyServerDropdownWhenNoServers()
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
                .ReturnsAsync(([new() { Id = 1, Name = StandardValues.ComponentValues.PC, IsDeleted = false }], true));

            RegisterServices();

            IRenderedComponent<RegisterAlert> cut = _Context.RenderComponent<RegisterAlert>();

            AngleSharp.Dom.IElement serverSelect = cut.Find("select");
            var options = serverSelect.QuerySelectorAll("option");

            Assert.AreEqual(1, options.Length);
            Assert.IsTrue(options[0].TextContent.Contains("Select an option"));
        }

        /// <summary>
        /// Checks whether the page shows a warning when no components are found.
        /// </summary>
        [TestMethod]
        public void TestRendersNoComponentsWarning()
        {
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync((new List<ComponentModel>(), true));

            RegisterServices();

            IRenderedComponent<RegisterAlert> cut = _Context.RenderComponent<RegisterAlert>();

            Assert.IsTrue(cut.Markup.Contains("No components found"));
        }

        /// <summary>
        /// Checks whether the reporter field shows the user's Discord name.
        /// </summary>
        [TestMethod]
        public void TestReporterFieldShowsDiscordName()
        {
            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((CreateServerPage(), true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync(([new() { Id = 1, Name = StandardValues.ComponentValues.PC, IsDeleted = false }], true));

            RegisterServices();

            IRenderedComponent<RegisterAlert> cut = _Context.RenderComponent<RegisterAlert>();

            AngleSharp.Dom.IElement reporterInput = cut.Find("input[readonly]");
            string reporterValue = reporterInput.GetAttribute("value") ?? string.Empty;

            Assert.AreEqual("Tester", reporterValue);
        }

        /// <summary>
        /// Checks whether the page shows the duplicate alert error.
        /// </summary>
        [TestMethod]
        public async Task TestRegisterClickShowsDuplicateError()
        {
            PagedResponseModel<AlertModel> existingAlerts = new()
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
                .ReturnsAsync((CreateServerPage(), true));
            _MockAPIClient.Setup(c => c.GetComponents())
                .ReturnsAsync(([new() { Id = 1, Name = StandardValues.ComponentValues.PC, IsDeleted = false }], true));
            _MockAPIClient.Setup(c => c.GetAlerts(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((existingAlerts, true));

            RegisterServices();

            IRenderedComponent<RegisterAlert> cut = _Context.RenderComponent<RegisterAlert>();

            cut.Find("select").Change("TestServer");
            cut.FindAll("select")[1].Change(StandardValues.ComponentValues.PC);
            cut.FindAll("select")[2].Change(StandardValues.StatusValues.Offline);

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.IsTrue(cut.Markup.Contains("There is already an alert registered for the given details."));
        }
    }
}
