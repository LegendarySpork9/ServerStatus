// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;
using System.Timers;
using Timer = System.Timers.Timer;
using ServerStatusCommon.Models.Responses;

namespace ServerStatusSite.Components.Pages
{
    public partial class Home : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IClock _Clock { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private SharedSettingsModel SharedSettings { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private List<ServerModel> Servers = [];
        private List<EventModel> PCEvents = [];
        private List<EventModel> ServerEvents = [];
        private List<EventModel> ConnectionEvents = [];

        private Timer RefreshTimer { get; set; } = new();
        private DateTime NextElapse;

        /// <summary>
        /// Configures the timer and loads the servers from the API.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            TimerFunction _timerFunction = new(_Clock);

            _Logger.LogMessage(StandardValues.LoggerValues.Info, "Opened Home Page");

            RefreshTimer = new()
            {
                AutoReset = false
            };
            RefreshTimer.Elapsed += async (sender, e) => await TimerElapsed(sender, e);

            _Logger.LogMessage(StandardValues.LoggerValues.Debug, $"Timer Duration: {SharedSettings.RefreshTime} minutes");

            Servers = await APIService.GetServers();
            PCEvents = await APIService.GetServerEvents("PC");
            ServerEvents = await APIService.GetServerEvents("Server");
            ConnectionEvents = await APIService.GetServerEvents("Connection");

            DateTime currentTime = _Clock.UtcNow;
            NextElapse = currentTime.AddMinutes(SharedSettings.RefreshTime).AddMilliseconds(-currentTime.Millisecond);

            RefreshTimer.Interval = _timerFunction.GetTimerInterval(NextElapse).TotalMilliseconds;
            RefreshTimer.Start();
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        private string GetStyle() => StyleConverter.GetTableDarkMode(User.DarkMode);

        /// <summary>
        /// Loads the servers from the API.
        /// </summary>
        private async Task TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            TimerFunction _timerFunction = new(_Clock);

            try
            {
                NextElapse = NextElapse.AddMinutes(SharedSettings.RefreshTime);
                Servers = await APIService.GetServers();

                await InvokeAsync(StateHasChanged);

                RefreshTimer.Interval = _timerFunction.GetTimerInterval(NextElapse).TotalMilliseconds;
                RefreshTimer.Start();
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(StandardValues.LoggerValues.Warning, ex.Message);
                _Logger.LogMessage(StandardValues.LoggerValues.Error, ex.ToString());
            }
        }
    }
}
