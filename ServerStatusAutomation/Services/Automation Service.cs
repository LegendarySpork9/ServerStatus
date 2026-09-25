// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Values;
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

        private readonly Dictionary<int, Timer> _ServerTimers = [];
        private readonly Dictionary<int, DateTime> _ServerNextElapse = [];

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
        /// Configures the API service logger.
        /// </summary>
        public void Setup()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Automation Service");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configured Automation Service");
        }

        /// <summary>
        /// Performs the first run for each server and starts per-server timers.
        /// </summary>
        public async Task Start()
        {
            List<ServerModel> servers = await _APIService.GetServers();

            foreach (ServerModel server in servers)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Starting timer for {server.Name} with interval {server.EventInterval} seconds");

                await RunForServer(server);

                TimerFunction _timerFunction = new(_Clock);
                DateTime currentTime = _Clock.UtcNow;
                DateTime nextElapse = currentTime.AddSeconds(server.EventInterval)
                    .AddMilliseconds(-currentTime.Millisecond);

                _ServerNextElapse[server.Id] = nextElapse;

                Timer timer = new()
                {
                    AutoReset = false,
                    Interval = _timerFunction.GetTimerInterval(nextElapse).TotalMilliseconds
                };

                int serverId = server.Id;
                string serverName = server.Name;
                int eventInterval = server.EventInterval;

                timer.Elapsed += async (sender, e) => await ServerTimerElapsed(
                    serverId,
                    serverName,
                    eventInterval);

                _ServerTimers[server.Id] = timer;
                timer.Start();
            }
        }

        /// <summary>
        /// Performs a run for a specific server then restarts its timer.
        /// </summary>
        private async Task ServerTimerElapsed(
            int serverId,
            string serverName,
            int eventInterval)
        {
            TimerFunction _timerFunction = new(_Clock);

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Timer Triggered for {serverName}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Token Expiry: {_APIService.ExpiryTime}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current Time: {_Clock.UtcNow}");

                _ServerNextElapse[serverId] = _ServerNextElapse[serverId].AddSeconds(eventInterval);

                List<ServerModel> servers = await _APIService.GetServers();
                ServerModel? server = servers.Find(c => c.Id == serverId);

                if (server != null)
                {
                    await RunForServer(server);
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Server {serverName} no longer found in API");
                }

                if (_ServerTimers.TryGetValue(serverId, out Timer? timer))
                {
                    timer.Interval = _timerFunction.GetTimerInterval(_ServerNextElapse[serverId]).TotalMilliseconds;
                    timer.Start();
                }
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
        /// Runs the status checks for a single server.
        /// </summary>
        private async Task RunForServer(ServerModel server)
        {
            DateTime runStartTime = _Clock.UtcNow;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Running Automatic Status Checks for {server.Name}");

            List<string> components = await _APIService.GetComponents();

            Dictionary<string, List<EventModel>> componentStatuses = [];

            foreach (string component in components)
            {
                componentStatuses[component] = await _APIService.GetServerEvents(component);
            }

            PagedResponseModel<AlertModel>? alerts = await _APIService.GetAlerts(1);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Checking Status for {server.Name}");

            DateTime refreshPeriod = runStartTime.AddSeconds(-server.EventInterval);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Refresh Period: {refreshPeriod} -> {runStartTime}");

            DateTime? downtime = null;
            int? duration = null;

            if (server.Downtime != null)
            {
                TimeSpan downtimeTime = TimeSpan.Parse(server.Downtime.Time);
                downtime = DateTime.SpecifyKind(
                    runStartTime.Date.Add(downtimeTime),
                    DateTimeKind.Utc);
                duration = server.Downtime.Duration;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Downtime Period: {downtime} -> {downtime.Value.AddSeconds(duration.Value)}");
            }

            foreach (var (componentName, statuses) in componentStatuses)
            {
                EventModel? status = statuses.Find(c => c.Server.Id == server.Id);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current {componentName} Status: {status?.Status ?? "No Status"}");

                if (status != null && (status.DateOccured < refreshPeriod || status.Status != StandardValues.StatusValues.Online))
                {
                    if (status.Status != StandardValues.StatusValues.Unknown && (status.Status == StandardValues.StatusValues.Online || status.DateOccured < refreshPeriod))
                    {
                        status.Status = StandardValues.StatusValues.Unknown;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Updated {componentName} Status to Unknown");
                    }

                    if (downtime == null || (status.DateOccured < downtime || status.DateOccured > downtime.Value.AddSeconds(duration.Value)))
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
                            GameVersion = server.GameVersion,
                            DateOccured = runStartTime
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
