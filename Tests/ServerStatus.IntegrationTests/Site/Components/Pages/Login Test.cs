// Copyright © - Unpublished - Toby Hunter
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Components.Pages;
using ServerStatusSite.Functions;
using System.Net;

namespace ServerStatus.IntegrationTests.Site.Components.Pages
{
    [TestClass]
    public class LoginTest
    {
        private Bunit.TestContext _Context = null!;

        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IAPIClient> _MockAPIClient = null!;
        private Mock<IClock> _MockClock = null!;
        private Mock<IHttpContextAccessor> _MockHttpContextAccessor = null!;

        [TestInitialize]
        public void Setup()
        {
            _Context = new Bunit.TestContext();
            _Context.JSInterop.Mode = JSRuntimeMode.Loose;

            _MockLogger = new Mock<ILoggerService>();
            _MockAPIClient = new Mock<IAPIClient>();
            _MockClock = new Mock<IClock>();
            _MockHttpContextAccessor = new Mock<IHttpContextAccessor>();

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

            _Context.Services.AddDataProtection();
            _Context.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();
            _Context.Services.AddSingleton(_MockLogger.Object);
            _Context.Services.AddSingleton<IHttpContextAccessor>(_MockHttpContextAccessor.Object);
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
                Id = 0,
                Username = "",
                Password = "",
                Scopes = [],
                Settings = []
            };
        }

        /// <summary>
        /// Checks whether the page renders the login form.
        /// </summary>
        [TestMethod]
        public void TestRendersLoginForm()
        {
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            Assert.IsTrue(cut.Markup.Contains("Username:"));
            Assert.IsTrue(cut.Markup.Contains("Password:"));
            Assert.IsTrue(cut.Markup.Contains("Login"));
            Assert.IsTrue(cut.Find("button.btn-primary") != null);
        }

        /// <summary>
        /// Checks whether the error message is hidden on initial render.
        /// </summary>
        [TestMethod]
        public void TestErrorMessageHiddenOnLoad()
        {
            _Context.Services.AddSingleton(CreateUser());

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            Assert.IsFalse(cut.Markup.Contains("login-fail"));
        }

        /// <summary>
        /// Checks whether a failed login shows the incorrect credentials message.
        /// </summary>
        [TestMethod]
        public async Task TestFailedLoginShowsIncorrectCredentials()
        {
            UserModel user = CreateUser();
            user.Username = "TestUser";
            user.Password = "WrongPassword";

            _Context.Services.AddSingleton(user);

            _MockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));

            PagedResponseModel<UserModel> pagedUsers = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Username = "TestUser",
                        Password = HashFunction.HashString("CorrectPassword"),
                        Scopes = ["User"],
                        Settings = []
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            _MockAPIClient.Setup(c => c.GetUsers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedUsers, true));

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.IsTrue(cut.Markup.Contains("Your username or password was incorrect."));
        }

        /// <summary>
        /// Checks whether a failed login shows an API error when authorisation fails.
        /// </summary>
        [TestMethod]
        public async Task TestFailedLoginWhenApiReturnsError()
        {
            UserModel user = CreateUser();
            user.Username = "TestUser";
            user.Password = "Password";

            _Context.Services.AddSingleton(user);

            ResponseModel apiResponse = new()
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "Invalid credentials"
            };

            _MockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, apiResponse));

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.IsTrue(cut.Markup.Contains("API returned"));
            Assert.IsTrue(cut.Markup.Contains("Invalid credentials"));
        }

        /// <summary>
        /// Checks whether a failed login shows an error when no users are returned.
        /// </summary>
        [TestMethod]
        public async Task TestFailedLoginWhenNoUsersReturned()
        {
            UserModel user = CreateUser();
            user.Username = "TestUser";
            user.Password = "Password";

            _Context.Services.AddSingleton(user);

            _MockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));

            PagedResponseModel<UserModel> emptyUsers = new()
            {
                Entries = [],
                EntryCount = 0,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 0,
                TotalCount = 0
            };

            _MockAPIClient.Setup(c => c.GetUsers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((emptyUsers, true));

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.IsTrue(cut.Markup.Contains("Failed to fetch users from the API."));
        }

        /// <summary>
        /// Checks whether a successful login navigates to the return URL.
        /// </summary>
        [TestMethod]
        public async Task TestSuccessfulLoginNavigatesToReturnUrl()
        {
            string hashedPassword = HashFunction.HashString("CorrectPassword");

            UserModel user = CreateUser();
            user.Username = "TestUser";
            user.Password = "CorrectPassword";

            _Context.Services.AddSingleton(user);

            _MockAPIClient.Setup(c => c.Authorise())
                .ReturnsAsync(((AuthenticationModel?)null, (ResponseModel?)null));

            PagedResponseModel<UserModel> pagedUsers = new()
            {
                Entries =
                [
                    new()
                    {
                        Id = 1,
                        Username = "TestUser",
                        Password = hashedPassword,
                        Scopes = ["User"],
                        Settings =
                        [
                            new() { Id = 1, Name = "DarkMode", Value = "False" },
                            new() { Id = 2, Name = "IsAdmin", Value = "False" },
                            new() { Id = 3, Name = "DiscordName", Value = "Tester" }
                        ]
                    }
                ],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 200,
                TotalPageCount = 1,
                TotalCount = 1
            };

            _MockAPIClient.Setup(c => c.GetUsers(It.IsAny<List<KeyValuePair<string, object>>>()))
                .ReturnsAsync((pagedUsers, true));

            _MockAPIClient.Setup(c => c.GetUserSettings(It.IsAny<int>()))
                .ReturnsAsync((new List<UserSettingModel>
                {
                    new()
                    {
                        Application = "ServerStatus",
                        Settings =
                        [
                            new() { Id = 1, Name = "DarkMode", Value = "False" },
                            new() { Id = 2, Name = "IsAdmin", Value = "False" },
                            new() { Id = 3, Name = "DiscordName", Value = "Tester" }
                        ]
                    }
                }, true));

            IRenderedComponent<Login> cut = _Context.RenderComponent<Login>();

            await cut.Find("button.btn-primary").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            NavigationManager nav = _Context.Services.GetRequiredService<NavigationManager>();
            Assert.IsTrue(nav.Uri.EndsWith("/"));
        }
    }
}
