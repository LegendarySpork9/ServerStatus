// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;

namespace ServerStatusSite.Components.Pages.Alerts
{
    public partial class RegisterAlert : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IHTTPClient _HTTPClient { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private SharedSettingsModel SharedSettings { get; set; } = default!;
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private List<ServerModel>? Servers;
        private List<string> ServersNames = [];
        private List<string> ComponentNames = [];

        private bool IsLoading;
        private bool RegisterSuccess;
        private bool ShowError;

        private string ErrorMessage { get; set; } = string.Empty;

        private string Server { get; set; } = string.Empty;
        private string Component { get; set; } = string.Empty;
        private string ComponentStatus { get; set; } = string.Empty;
        private string DiscordName { get; set;  } = string.Empty;

        /// <summary>
        /// Loads the servers from the API.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Register Alerts Page");

            IsLoading = true;

            Servers = await APIService.GetServers();
            ComponentNames = await APIService.GetComponents();

            if (Servers != null)
            {
                ServersNames.AddRange(Servers.Select(s => s.Name));
            }

            DiscordName = User.Settings.Find(s => s.Name == "DiscordName")?.Value ?? User.Username;

            IsLoading = false;
        }

        /// <summary>
        /// Returns the css to change the page to dark mode.
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
        /// Adds an alert to the API.
        /// </summary>
        private async Task RegisterClick()
        {
            ShowError = false;
            RegisterSuccess = false;
            ErrorMessage = string.Empty;
            IsLoading = true;

            StateHasChanged();

            AlertInformationModel? alerts = await APIService.GetAlerts(1);

            if (Servers != null && alerts != null)
            {
                ServerModel server = Servers.First(s => s.Name == Server);
                AlertModel? alert = alerts.Entries.Find(c => c.Server.Id == server.Id && c.Component == Component && c.AlertStatus != "Resolved");

                if (alert == null)
                {
                    DiscordService _discordService = new(
                        _Logger,
                        _HTTPClient,
                        SharedSettings);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Attempting Alert Register");

                    SettingModel discordSetting = User.Settings.First(s => s.Name == "DiscordName");

                    string[] gameDetails = Server.Split('(');

                    AlertRequestModel alertRequest = new()
                    {
                        Reporter = discordSetting.Value,
                        Component = Component,
                        ComponentStatus = ComponentStatus,
                        AlertStatus = "Reported",
                        ServerId = server.Id,
                        Name = server.Name,
                        HostName = server.HostName,
                        Game = server.Game,
                        GameVersion = server.GameVersion
                    };

                    (AlertModel? newAlert, ResponseModel? apiResponse) = await APIService.RegisterAlert(alertRequest);

                    if (newAlert != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            "Alert Registered");

                        if (SharedSettings.RecipientIds.Contains(','))
                        {
                            await _discordService.SendNotification(
                                SharedSettings.RecipientIds.Split(',')[0],
                                $"{discordSetting.Value} has reported an issue with the {Server} server. {Component}: {ComponentStatus}");
                        }

                        else
                        {
                            await _discordService.SendNotification(
                                SharedSettings.RecipientIds,
                                $"{discordSetting.Value} has reported an issue with the {Server} server. {Component}: {ComponentStatus}");
                        }

                        RegisterSuccess = true;
                    }

                    else
                    {
                        ErrorMessage = $"API returned {apiResponse?.StatusCode} ({apiResponse?.Message})";

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            ErrorMessage);
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Alert Register Complete");
                }

                else
                {
                    ShowError = true;
                }
            }

            IsLoading = false;

            await InvokeAsync(StateHasChanged);

            if (RegisterSuccess)
            {
                await Task.Delay(2000).ContinueWith(_ =>
                {
                    Navigation.NavigateTo("/alerts");
                });
            }
        }
    }
}