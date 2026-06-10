// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Functions;

namespace ServerStatusSite.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;
        [Inject]
        private ProtectedSessionStorage SessionStorage { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private string ReturnUrl { get; set; } = "/";
        private bool ShowError { get; set; } = false;
        private bool Loading { get; set; } = false;

        /// <summary>
        /// Captures the URL the user was trying to access and sets the API _Logger.
        /// </summary>
        protected override void OnInitialized()
        {
            _Logger.ChangeIdentifier(IPAddressFunction.FetchIpAddress(HttpContextAccessor));
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Login Page");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Url: {Navigation.Uri}");

            Uri uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue(
                "returnUrl",
                out var returnUrl))
            {
                ReturnUrl = returnUrl.ToString() ?? "/";

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Return Url: {ReturnUrl}");
            }

            APIService.SetLogger(_Logger);
        }

        /// <summary>
        /// Checks the user details and sends the user to the return URL.
        /// </summary>
        private async Task LoginClick()
        {
            Loading = true;
            StateHasChanged();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Attempting Login");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Username: {User.Username}");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Password: {User.Password}");

            await APIService.Authorise();
            List<UserModel> users = await APIService.GetUsers();
            UserModel? user = users.Find(c => c.Username == User.Username && c.Password == HashFunction.HashString(User.Password));

            if (user != null)
            {
                _Logger.LogMessage(StandardValues.LoggerValues.Info, $"Login Successful.");
                _Logger.ChangeIdentifier($"{user.Username} ({IPAddressFunction.FetchIpAddress(HttpContextAccessor)})");
                APIService.SetLogger(_Logger);
                user = await APIService.GetUserSettings(user);

                User.Id = user.Id;
                User.Username = user.Username;
                User.Password = user.Password;
                User.Scopes = user.Scopes;
                User.Settings = user.Settings;

                await SessionStorage.SetAsync(
                        "loggedInUser",
                        User);

                Navigation.NavigateTo(ReturnUrl);
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Login Failed.");
                ShowError = true;
            }

            Loading = false;
        }
    }
}
