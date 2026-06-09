// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusSite.Components.Pages.Alerts
{
    public partial class EditAlert : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IHTTPClient _HTTPClient { get; set; } = default!;
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private SharedSettingsModel SharedSettings { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private AlertModel Alert = StandardValues.AlertValues.DefaultAlert;
        private int AlertId { get; set; } = 0;
        private bool Loading { get; set; } = false;

        /// <summary>
        /// Gets the data about the given alert.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            _Logger.LogMessage(StandardValues.LoggerValues.Info, "Opened Edit Alerts Page");
            _Logger.LogMessage(StandardValues.LoggerValues.Debug, $"Url: {Navigation.Uri}");

            Uri uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("alertId", out var alertId))
            {
                AlertId = int.Parse(alertId.ToString() ?? "0");

                _Logger.LogMessage(StandardValues.LoggerValues.Debug, $"Alert Id: {AlertId}");
            }

            Alert = await APIService.GetAlert(AlertId) ?? StandardValues.AlertValues.DefaultAlert;
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        public string GetStyle(string component)
        {
            return component switch
            {
                "Form" => StyleConverter.GetFormDarkMode(User.DarkMode),
                "Input" => StyleConverter.GetInputDarkMode(User.DarkMode),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Updates the alert data then sends the user back to the alerts page. 
        /// </summary>
        private async Task SaveClick()
        {
            Loading = true;
            StateHasChanged();

            DiscordService _discordService = new(_Logger, _HTTPClient, SharedSettings);

            _Logger.LogMessage(StandardValues.LoggerValues.Info, "Attempting Alert Save");

            AlertUpdateRequestModel alertUpdate = new()
            {
                Status = Alert.AlertStatus
            };

            (AlertModel? alert, ResponseModel? apiResponse) = await APIService.UpdateAlert(
                AlertId,
                alertUpdate);

            if (alert != null)
            {
                _Logger.LogMessage(StandardValues.LoggerValues.Debug, "Alert Status Updated");
            }

            _Logger.LogMessage(StandardValues.LoggerValues.Info, "Alert Save Complete");

            SettingModel discordSetting = User.Settings.First(s => s.Name == "DiscordName");

            if (SharedSettings.RecipientIds.Contains(','))
            {
                await _discordService.SendNotification(SharedSettings.RecipientIds.Split(',')[1], $"{discordSetting.Value} has updated the alert for {Alert.Server} - {Alert.Component} to the status {Alert.AlertStatus}.");
            }

            else
            {
                await _discordService.SendNotification(SharedSettings.RecipientIds, $"{discordSetting.Value} has updated the alert for {Alert.Server} - {Alert.Component} to the status {Alert.AlertStatus}.");
            }

            Navigation.NavigateTo("/alerts");
        }
    }
}