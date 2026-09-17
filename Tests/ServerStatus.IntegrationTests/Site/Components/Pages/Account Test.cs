// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Components.Pages;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class AccountTest
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

            RetryService retryService = new(_MockLogger.Object);
            APIService apiService = new(
                _MockLogger.Object,
                _MockAPIClient.Object,
                _MockClock.Object,
                retryService)
            {
                ExpiryTime = new DateTime(2026, 09, 01, 16, 0, 0, DateTimeKind.Utc)
            };

            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton(apiService);
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
        /// Checks whether the page renders the user's details.
        /// </summary>
        [TestMethod]
        public void TestRendersUserDetails()
        {
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            Assert.IsTrue(cut.Markup.Contains("Username:"));
            Assert.IsTrue(cut.Markup.Contains("Password:"));
            Assert.IsTrue(cut.Markup.Contains("Discord Name:"));
            Assert.IsTrue(cut.Markup.Contains("Dark Mode:"));
        }

        /// <summary>
        /// Checks whether the page contains a Save button.
        /// </summary>
        [TestMethod]
        public void TestRendersSaveButton()
        {
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            AngleSharp.Dom.IElement saveButton = cut.Find("button.btn-primary");

            Assert.IsTrue(saveButton.TextContent.Contains("Save"));
        }
    }
}
