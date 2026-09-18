// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Responses;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Components.Pages;
using ServerStatusSite.Models;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class ConfigurationTest
    {
        private Bunit.TestContext _Context = null!;

        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IWebHostEnvironment> _MockEnvironment = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;

        [TestInitialize]
        public void Setup()
        {
            _Context = new Bunit.TestContext();

            _MockLogger = new Mock<ILoggerService>();
            _MockEnvironment = new Mock<IWebHostEnvironment>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();

            _MockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
            _MockEnvironment.Setup(e => e.ContentRootPath).Returns("C:\\Test");

            _MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync("{\"BackupToolAPI\":{\"WebhookSecret\":\"\",\"Servers\":{}}}");
            _MockFileSystem.Setup(fs => fs.WriteAllText(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IWebHostEnvironment>(_MockEnvironment.Object);
            _Context.Services.AddSingleton<IExtendedFileSystem>(_MockFileSystem.Object);
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

        /// <summary>
        /// Checks whether the page renders correctly for an admin user.
        /// </summary>
        [TestMethod]
        public void TestRendersForAdminUser()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            cut.Find("h5");

            Assert.IsTrue(cut.Markup.Contains("Webhook Secret"));
        }

        /// <summary>
        /// Checks whether the page shows an unauthorised message for a non-admin user.
        /// </summary>
        [TestMethod]
        public void TestShowsUnauthorisedForNonAdmin()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateNonAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(cut.Markup.Contains("not authorised"));
        }

        /// <summary>
        /// Checks whether the page shows "Not set" when no webhook secret is configured.
        /// </summary>
        [TestMethod]
        public void TestShowsNotSetWhenNoWebhookSecret()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(cut.Markup.Contains("Not set"));
            Assert.IsTrue(cut.Markup.Contains("Generate"));
        }

        /// <summary>
        /// Checks whether the page shows masked text and Regenerate button when a webhook secret exists.
        /// </summary>
        [TestMethod]
        public void TestShowsMaskedWebhookSecretWhenSet()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "existing-secret",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(cut.Markup.Contains("********"));
            Assert.IsTrue(cut.Markup.Contains("Regenerate"));
        }

        /// <summary>
        /// Checks whether the page shows "No servers configured" when the server list is empty.
        /// </summary>
        [TestMethod]
        public void TestShowsNoServersMessage()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(cut.Markup.Contains("No servers configured"));
        }

        /// <summary>
        /// Checks whether the page displays configured servers in the table with masked credentials.
        /// </summary>
        [TestMethod]
        public void TestDisplaysConfiguredServers()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com",
                Servers = new()
                {
                    ["TestServer"] = new() { ClientId = "client-id", ClientSecret = "client-secret" }
                }
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(cut.Markup.Contains("TestServer"));
            Assert.IsFalse(cut.Markup.Contains("client-id"));
            Assert.IsFalse(cut.Markup.Contains("client-secret"));
        }

        /// <summary>
        /// Checks whether clicking Add Server shows the add form.
        /// </summary>
        [TestMethod]
        public void TestAddServerButtonShowsForm()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsFalse(
                cut.Markup.Contains("Server Name:"));

            AngleSharp.Dom.IElement addServerButton = cut.FindAll("button.btn-primary")
                .First(b => b.TextContent.Trim() == "Add Server");
            addServerButton.Click();

            Assert.IsTrue(cut.Markup.Contains("Server Name:"));
            Assert.IsTrue(cut.Markup.Contains("Client ID:"));
            Assert.IsTrue(cut.Markup.Contains("Client Secret:"));
        }

        /// <summary>
        /// Checks whether adding a server with empty name shows a validation error.
        /// </summary>
        [TestMethod]
        public void TestAddServerValidatesEmptyName()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            AngleSharp.Dom.IElement addServerButton = cut.FindAll("button.btn-primary")
                .First(b => b.TextContent.Trim() == "Add Server");
            addServerButton.Click();

            AngleSharp.Dom.IElement addButton = cut.FindAll("button.btn-primary")
                .First(b => b.TextContent.Trim() == "Add");
            addButton.Click();

            Assert.IsTrue(cut.Markup.Contains("Server name is required"));
        }

        /// <summary>
        /// Checks whether the Generate button generates a webhook secret and shows it for copying.
        /// </summary>
        [TestMethod]
        public void TestGenerateWebhookSecret()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com"
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            _Context.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            AngleSharp.Dom.IElement generateButton = cut.FindAll("button.btn-primary.btn-sm")
                .First(b => b.TextContent.Trim() == "Generate");
            generateButton.Click();

            Assert.IsFalse(string.IsNullOrEmpty(settings.WebhookSecret));
            Assert.AreEqual(
                64,
                settings.WebhookSecret.Length);
            Assert.IsTrue(cut.Markup.Contains("Copy"));

            AngleSharp.Dom.IElement input = cut.Find("input[readonly]");
            string displayedValue = input.GetAttribute("value") ?? string.Empty;

            Assert.AreEqual(
                64,
                displayedValue.Length);
            Assert.AreNotEqual(
                settings.WebhookSecret,
                displayedValue);
        }

        /// <summary>
        /// Checks whether clicking Edit on a server row shows input fields.
        /// </summary>
        [TestMethod]
        public void TestEditServerShowsInputFields()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com",
                Servers = new()
                {
                    ["TestServer"] = new() { ClientId = "client-id", ClientSecret = "client-secret" }
                }
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            AngleSharp.Dom.IElement editButton = cut.FindAll("button.btn-primary.btn-sm")
                .First(b => b.TextContent.Trim() == "Edit");
            editButton.Click();

            Assert.IsTrue(cut.Markup.Contains("Save"));
            Assert.IsTrue(cut.Markup.Contains("Cancel"));
        }

        /// <summary>
        /// Checks whether clicking Remove on a server row removes it from the list.
        /// </summary>
        [TestMethod]
        public void TestRemoveServerRemovesFromList()
        {
            BackupToolSettingsModel settings = new()
            {
                APIURLTemplate = "https://{0}.example.com/api",
                WebhookSecret = "",
                SiteBaseURL = "https://example.com",
                Servers = new()
                {
                    ["TestServer"] = new() { ClientId = "client-id", ClientSecret = "client-secret" }
                }
            };

            _Context.Services.AddSingleton(settings);
            _Context.Services.AddSingleton(CreateAdminUser());

            IRenderedComponent<Configuration> cut = _Context.RenderComponent<Configuration>();

            Assert.IsTrue(
                cut.Markup.Contains("TestServer"));

            AngleSharp.Dom.IElement removeButton = cut.Find("button.btn-danger.btn-sm");
            removeButton.Click();

            Assert.IsFalse(cut.Markup.Contains("TestServer"));
            Assert.AreEqual(
                0,
                settings.Servers.Count(s => !string.IsNullOrWhiteSpace(s.Key)));
        }
    }
}
