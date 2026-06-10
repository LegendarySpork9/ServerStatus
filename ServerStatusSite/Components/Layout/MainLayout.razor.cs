// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ServerStatusCommon.Models.Responses;
using ServerStatusSite.Converters;

namespace ServerStatusSite.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;
        [Inject]
        private ProtectedSessionStorage SessionStorage { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private bool IsInitialised;

        /// <summary>
        /// Subscribes the layout to the DarkMode event.
        /// </summary>
        protected override void OnInitialized()
        {
            User.OnDarkModeChanged += StateHasChanged;
        }

        /// <summary>
        /// Checks if the user is logged in.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                string returnUrl = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
                string loginUrl = $"/login?returnUrl={returnUrl}";

                try
                {
                    ProtectedBrowserStorageResult<UserModel> userResult = await SessionStorage.GetAsync<UserModel>("loggedInUser");

                    if (userResult.Success && userResult.Value != null)
                    {
                        if (User.Id == 0)
                        {
                            User.Id = userResult.Value.Id;
                            User.Username = userResult.Value.Username;
                            User.Password = userResult.Value.Password;
                            User.Scopes = userResult.Value.Scopes;
                            User.Settings = userResult.Value.Settings;
                            User.DarkMode = User.DarkMode;
                        }

                        IsInitialised = true;
                        StateHasChanged();
                    }

                    else
                    {
                        Navigation.NavigateTo(loginUrl);
                    }
                }

                catch
                {
                    Navigation.NavigateTo(loginUrl);
                }
            }
        }

        /// <summary>
        /// Returns the CSS to change the layout to dark mode.
        /// </summary>
        private string GetStyle(string component)
        {
            return component switch
            {
                "Body" => StyleConverter.GetBodyDarkMode(User.DarkMode),
                "Bar" => StyleConverter.GetTopBarDarkMode(User.DarkMode),
                "Link" => StyleConverter.GetTopNavLinkDarkMode(User.DarkMode),
                _ => String.Empty
            };
        }

        /// <summary>
        /// Unsubscribes the layout from the DarkMode event.
        /// </summary>
        public void Dispose()
        {
            User.OnDarkModeChanged -= StateHasChanged;
        }
    }
}
