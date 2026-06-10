// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerStatusSite.Components.Pages.Alerts
{
    public partial class Alerts : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IClock _Clock { get; set; } = default!;
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private SharedSettingsModel SharedSettings { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private AlertInformationModel? ReportedAlerts;

        private bool IsLoading;

        private Timer RefreshTimer { get; set; } = new();
        private DateTime NextElapse;
        private int PageNumber = 1;

        /// <summary>
        /// Configures the timer and loads the alerts from the API.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            TimerFunction _timerFunction = new(_Clock);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Alerts Page");

            IsLoading = true;

            RefreshTimer = new()
            {
                AutoReset = false
            };
            RefreshTimer.Elapsed += async (sender, e) => await TimerElapsed(sender, e);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Timer Duration: {SharedSettings.RefreshTime} minutes");

            ReportedAlerts = await APIService.GetAlerts(PageNumber) ?? StandardValues.AlertValues.DefaultAlertInfo;

            DateTime currentTime = _Clock.UtcNow;
            NextElapse = currentTime.AddMinutes(SharedSettings.RefreshTime)
                .AddMilliseconds(-currentTime.Millisecond);

            RefreshTimer.Interval = _timerFunction.GetTimerInterval(NextElapse).TotalMilliseconds;
            RefreshTimer.Start();

            IsLoading = false;
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        private string GetStyle(string? component = null)
        {
            return component switch
            {
                "Data" => StyleConverter.GetTableRowDarkMode(User.DarkMode),
                _ => StyleConverter.GetTableDarkMode(User.DarkMode)
            };
        }

        /// <summary>
        /// Sends the user to the register alert page.
        /// </summary>
        private void RegisterAlert()
        {
            Navigation.NavigateTo("/registeralert");
        }

        /// <summary>
        /// Loads the previous page of alerts from the API.
        /// </summary>
        private async Task PreviousPage()
        {
            PageNumber--;

            ReportedAlerts = await APIService.GetAlerts(PageNumber);
        }

        /// <summary>
        /// Loads the next page of alerts from the API.
        /// </summary>
        private async Task NextPage()
        {
            PageNumber++;

            ReportedAlerts = await APIService.GetAlerts(PageNumber);
        }

        /// <summary>
        /// Sends the user to the edit alert page.
        /// </summary>
        private void OpenClick(AlertModel alert)
        {
            SettingModel adminSetting = User.Settings.First(s => s.Name == "IsAdmin");

            if (bool.Parse(adminSetting.Value))
            {
                Navigation.NavigateTo($"/editalert?alertId={alert.Id}");
            }
        }

        /// <summary>
        /// Loads the alerts from the API.
        /// </summary>
        private async Task TimerElapsed(
            object? sender,
            ElapsedEventArgs e)
        {
            TimerFunction _timerFunction = new(_Clock);

            try
            {
                NextElapse = NextElapse.AddMinutes(SharedSettings.RefreshTime);
                ReportedAlerts = await APIService.GetAlerts(PageNumber);

                await InvokeAsync(StateHasChanged);

                RefreshTimer.Interval = _timerFunction.GetTimerInterval(NextElapse).TotalMilliseconds;
                RefreshTimer.Start();
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    ex.Message);
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());
            }
        }
    }
}
