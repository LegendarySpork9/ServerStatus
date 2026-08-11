// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerStatusAutomation.Services
{
    public class AutomationService
    {
        private readonly ILoggerService _Logger;
        private readonly IClock _Clock;
        private readonly IHTTPClient _HTTPClient;
        private readonly APIService _APIService;
        private readonly SharedSettingsModel SharedSettings;

        private Timer RefreshTimer;
        private DateTime NextElapse;

        // Sets the class's global variables.
        public AutomationService(
            ILoggerService _logger,
            IClock _clock,
            IHTTPClient _httpClient,
            APIService _apiService,
            SharedSettingsModel sharedSettings)
        {
            _Logger = _logger;
            _Clock = _clock;
            _HTTPClient = _httpClient;
            _APIService = _apiService;
            SharedSettings = sharedSettings;
        }

        /// <summary>
        /// Configures the timer and API service logger.
        /// </summary>
        public void Setup()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Automation Service");

            RefreshTimer = new()
            {
                AutoReset = false
            };
            RefreshTimer.Elapsed += async (sender, e) => await TimerElapsed(sender, e);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Timer Duration: {SharedSettings.RefreshTime} minutes");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configured Automation Service");
        }

        // Performs the first run and starts the timer.
        public async Task Start()
        {
            TimerFunction _timerFunction = new(_Clock);

            await Run();

            DateTime currentTime = _Clock.UtcNow;
            NextElapse = currentTime.AddMinutes(SharedSettings.RefreshTime)
                .AddMilliseconds(-currentTime.Millisecond);

            RefreshTimer.Interval = _timerFunction.GetTimerInterval(NextElapse).TotalMilliseconds;
            RefreshTimer.Start();
        }

        // Performs a run then restarts the timer.
        private async Task TimerElapsed(
            object? sender,
            ElapsedEventArgs e)
        {
            TimerFunction _timerFunction = new(_Clock);

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Timer Triggered");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Token Expiry: {_APIService.ExpiryTime}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current Time: {_Clock.UtcNow}");

                NextElapse = NextElapse.AddMinutes(SharedSettings.RefreshTime);

                await Run();

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

        /// <summary>
        /// Runs the status checks.
        /// </summary>
        private async Task Run()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Running Automatic Status Checks");

            List<ServerModel> servers = await _APIService.GetServers();
            List<string> components = await _APIService.GetComponents();

            Dictionary<string, List<EventModel>> componentStatuses = [];

            foreach (string component in components)
            {
                componentStatuses[component] = await _APIService.GetServerEvents(component);
            }

            AlertInformationModel? alerts = await _APIService.GetAlerts(1);

            foreach (ServerModel server in servers)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Checking Status for {server.Name}");

                DateTime now = _Clock.UtcNow;
                DateTime refreshPeriod = now.AddMinutes(-server.EventInterval);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Refresh Period: {refreshPeriod} -> {now}");

                DateTime? downtime = null;
                int? duration = null;

                if (server.Downtime != null)
                {
                    DateTime time = DateTime.SpecifyKind(
                        DateTime.Parse(server.Downtime.Time),
                        DateTimeKind.Utc);

                    if (time < _Clock.UtcNow)
                    {
                        downtime = time.AddDays(1);
                    }

                    else
                    {
                        downtime = time;
                    }

                    duration = server.Downtime.Duration;

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Downtime Period: {downtime} -> {downtime.Value.AddMinutes(duration.Value)}");
                }

                foreach (var (componentName, statuses) in componentStatuses)
                {
                    EventModel? status = statuses.Find(c => c.Server.Id == server.Id);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Current {componentName} Status: {status?.Status ?? "No Status"}");

                    if (status != null && (status.DateOccured < refreshPeriod || status.Status != "Online"))
                    {
                        if (status.Status != "Unknown" && (status.Status == "Online" || status.DateOccured < refreshPeriod))
                        {
                            status.Status = "Unknown";

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Updated {componentName} Status to Unknown");
                        }

                        if (downtime == null || (status.DateOccured < downtime || status.DateOccured > downtime.Value.AddMinutes(duration.Value)))
                        {
                            await AlertsHandler(
                                alerts?.Entries ?? [],
                                server,
                                status.Component,
                                status.Status);
                        }

                        if (status.DateOccured < refreshPeriod)
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = status.Component,
                                Status = status.Status,
                                ServerId = server.Id,
                                Name = server.Name,
                                HostName = server.HostName,
                                Game = server.Game,
                                GameVersion = server.GameVersion
                            };

                            (EventModel? createdEvent, ResponseModel? apiResponse) = await _APIService.RegisterServerEvent(newEvent);

                            if (createdEvent != null)
                            {
                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Debug,
                                    "Server Event Registered");
                            }
                        }
                    }
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Checked Status for {server.Name}");
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Ran Automatic Status Checks");
        }

        /// <summary>
        /// Raises an alert if an unresolved one is not found.
        /// </summary>
        private async Task AlertsHandler(
            List<AlertModel> alerts,
            ServerModel server,
            string component,
            string status)
        {
            DiscordService _discordService = new(
                _Logger,
                _HTTPClient,
                SharedSettings);

            bool alertFound = false;

            foreach (AlertModel alert in alerts)
            {
                if (alert.Server.Id == server.Id && alert.Component == component)
                {
                    alertFound = true;

                    if (alert.AlertStatus == "Resolved")
                    {
                        AlertRequestModel newAlert = new()
                        {
                            Reporter = "Automation",
                            Component = component,
                            ComponentStatus = status,
                            AlertStatus = "Reported",
                            ServerId = server.Id,
                            Name = server.Name,
                            HostName = server.HostName,
                            Game = server.Game,
                            GameVersion = server.GameVersion
                        };

                        (AlertModel? createdAlert, ResponseModel? apiResponse) = await _APIService.RegisterAlert(newAlert);

                        if (createdAlert != null)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                "Alert Registered");

                            await _discordService.SendNotification(
                                server.WebhookURL,
                                SharedSettings.RecipientId,
                                $"Automation has reported an issue with the {server.Name} server. {component}: {status}");
                        }
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            "Existing Alert Found");
                    }

                    break;
                }
            }

            if (!alertFound)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "No Alerts Found in API");

                AlertRequestModel newAlert = new()
                {
                    Reporter = "Automation",
                    Component = component,
                    ComponentStatus = status,
                    AlertStatus = "Reported",
                    ServerId = server.Id,
                    Name = server.Name,
                    HostName = server.HostName,
                    Game = server.Game,
                    GameVersion = server.GameVersion
                };

                (AlertModel? createdAlert, ResponseModel? apiResponse) = await _APIService.RegisterAlert(newAlert);

                if (createdAlert != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Alert Registered");

                    await _discordService.SendNotification(
                        server.WebhookURL,
                        SharedSettings.RecipientId,
                        $"Automation has reported an issue with the {server.Name} server. {component}: {status}");
                }
            }
        }
    }
}
