// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Models.Responses;
using ServerStatusSite.Converters;

namespace ServerStatusSite.Components.Layout
{
    public partial class NavMenu
    {
        [Inject]
        private UserModel User { get; set; } = default!;

        /// <summary>
        /// Subscribes the layout to the DarkMode event.
        /// </summary>
        protected override void OnInitialized()
        {
            User.OnDarkModeChanged += StateHasChanged;
        }

        /// <summary>
        /// Returns the CSS to change the menu to dark mode.
        /// </summary>
        private string GetStyle(string? component = null)
        {
            return component switch
            {
                "Corner" => StyleConverter.GetTopBarDarkMode(User.DarkMode),
                _ => StyleConverter.GetNavMenuDarkMode(User.DarkMode)
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
