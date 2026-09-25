// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Components.Pages;
using ServerStatusSite.Models;
using ServerStatusSite.Services;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class ServerLogsTest
    {
        private Bunit.TestContext _Context = null!;

        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IAPIClient> _MockAPIClient = null!;
        private Mock<IClock> _MockClock = null!;
        private Mock<IBackupToolAPIClient> _MockBackupToolClient = null!;

        [TestInitialize]
        public void Setup()
        {
            _Context = new Bunit.TestContext();

            _MockLogger = new Mock<ILoggerService>();
            _MockAPIClient = new Mock<IAPIClient>();
            _MockClock = new Mock<IClock>();
            _MockBackupToolClient = new Mock<IBackupToolAPIClient>();

            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _Context.Dispose();
        }

        private UserModel CreateAdminUser()
        {
            return new()
            {
                Id = 1,
                Username = "Admin",
                Password = "HashedString",
                Scopes = ["User"],
                Settings =
                [
                    new() { Id = 1, Name = "IsAdmin", Value = "True" },
                    new() { Id = 2, Name = "DarkMode", Value = "False" },
                    new() { Id = 3, Name = "DiscordName", Value = "Admin" }
                ]
            };
        }

        private UserModel CreateNonAdminUser()
        {
            return new()
            {
                Id = 2,
                Username = "User",
                Password = "HashedString",
                Scopes = ["User"],
                Settings =
                [
                    new() { Id = 1, Name = "IsAdmin", Value = "False" },
                    new() { Id = 2, Name = "DarkMode", Value = "False" },
                    new() { Id = 3, Name = "DiscordName", Value = "User" }
                ]
            };
        }

        private void RegisterServices(UserModel user)
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

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            BackupToolAPIService backupToolApiService = new(
                _MockLogger.Object,
                _MockBackupToolClient.Object,
                retryService);

            LogStreamService logStreamService = new();

            BackupToolSettingsModel backupToolSettings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "test-secret",
                SiteBaseURL = "https://example.com",
                Servers = new()
                {
                    ["TestServer"] = new() { ClientId = "id", ClientSecret = "secret" }
                }
            };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(backupToolApiService);
            _Context.Services.AddSingleton(logStreamService);
            _Context.Services.AddSingleton(backupToolSettings);
            _Context.Services.AddSingleton(user);
        }

        /// <summary>
        /// Checks whether the page shows an unauthorised message for a non-admin user.
        /// </summary>
        [TestMethod]
        public void TestShowsUnauthorisedForNonAdmin()
        {
            RegisterServices(CreateNonAdminUser());

            IRenderedComponent<ServerLogs> cut = _Context.RenderComponent<ServerLogs>();

            Assert.IsTrue(cut.Markup.Contains("not authorised"));
        }

        /// <summary>
        /// Checks whether the page renders the server dropdown for an admin user.
        /// </summary>
        [TestMethod]
        public void TestRendersServerDropdownForAdmin()
        {
            RegisterServices(CreateAdminUser());

            IRenderedComponent<ServerLogs> cut = _Context.RenderComponent<ServerLogs>();

            Assert.IsTrue(cut.Markup.Contains("TestServer"));
            Assert.IsFalse(cut.Markup.Contains("not authorised"));
        }

        /// <summary>
        /// Checks whether the page only shows servers that are configured in BackupToolAPI settings.
        /// </summary>
        [TestMethod]
        public void TestOnlyShowsConfiguredServers()
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
                    },
                    new()
                    {
                        Id = 2,
                        Name = "UnconfiguredServer",
                        HostName = "other-host",
                        Game = "Terraria",
                        GameVersion = "1.4",
                        Connection = new() { IPAddress = "127.0.0.1", Port = 7777 },
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

            _MockAPIClient.Setup(c => c.GetServers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedServers, true));

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            BackupToolAPIService backupToolApiService = new(
                _MockLogger.Object,
                _MockBackupToolClient.Object,
                retryService);

            LogStreamService logStreamService = new();

            BackupToolSettingsModel backupToolSettings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "test-secret",
                SiteBaseURL = "https://example.com",
                Servers = new()
                {
                    ["TestServer"] = new() { ClientId = "id", ClientSecret = "secret" }
                }
            };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton(apiService);
            _Context.Services.AddSingleton(backupToolApiService);
            _Context.Services.AddSingleton(logStreamService);
            _Context.Services.AddSingleton(backupToolSettings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<ServerLogs> cut = _Context.RenderComponent<ServerLogs>();

            Assert.IsTrue(cut.Markup.Contains("TestServer"));
            Assert.IsFalse(cut.Markup.Contains("UnconfiguredServer"));
        }
    }
}
