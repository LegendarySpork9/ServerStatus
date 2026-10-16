// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Components.Pages;
using System.Net;

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

        /// <summary>
        /// Checks whether saving with a changed username calls UpdateUser.
        /// </summary>
        [TestMethod]
        public async Task TestSaveClickUpdatesUsername()
        {
            UserModel updatedUser = new()
            {
                Id = 1,
                Username = "NewUsername",
                Password = "HashedPassword",
                Scopes = ["User"],
                Settings = []
            };

            _MockAPIClient.Setup(c => c.UpdateUser(
                    It.IsAny<int>(),
                    It.IsAny<UserUpdateRequestModel>()))
                .ReturnsAsync((updatedUser, (ResponseModel?)null));

            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            cut.Find("input[type='text']").Change("NewUsername");

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            _MockAPIClient.Verify(c => c.UpdateUser(
                1,
                It.Is<UserUpdateRequestModel>(u => u.Username == "NewUsername")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether saving shows success message when all updates succeed.
        /// </summary>
        [TestMethod]
        public void TestSaveClickShowsSuccessMessage()
        {
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            cut.Find("button.btn-primary").Click();

            cut.WaitForAssertion(() => Assert.IsTrue(cut.Markup.Contains("Saved successfully!")));
        }

        /// <summary>
        /// Checks whether saving shows an error message when UpdateUser fails.
        /// </summary>
        [TestMethod]
        public async Task TestSaveClickShowsErrorOnApiFailure()
        {
            ResponseModel apiResponse = new()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Message = "Server error"
            };

            _MockAPIClient.Setup(c => c.UpdateUser(
                    It.IsAny<int>(),
                    It.IsAny<UserUpdateRequestModel>()))
                .ReturnsAsync(((UserModel?)null, apiResponse));

            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            cut.Find("input[type='text']").Change("NewUsername");

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.IsTrue(cut.Markup.Contains("API returned"));
            Assert.IsTrue(cut.Markup.Contains("Server error"));
        }

        /// <summary>
        /// Checks whether saving with a changed Discord name calls UpdateUserSettings.
        /// </summary>
        [TestMethod]
        public async Task TestSaveClickUpdatesDiscordName()
        {
            SettingModel updatedSetting = new()
            {
                Id = 3,
                Name = "DiscordName",
                Value = "NewDiscord"
            };

            _MockAPIClient.Setup(c => c.UpdateUserSettings(
                    It.IsAny<int>(),
                    It.IsAny<UserSettingUpdateRequestModel>()))
                .ReturnsAsync((updatedSetting, (ResponseModel?)null));

            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Account> cut = _Context.RenderComponent<Account>();

            var textInputs = cut.FindAll("input[type='text']");
            textInputs[1].Change("NewDiscord");

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            _MockAPIClient.Verify(c => c.UpdateUserSettings(
                3,
                It.Is<UserSettingUpdateRequestModel>(s => s.Value == "NewDiscord")),
                Times.Once);
        }
    }
}
