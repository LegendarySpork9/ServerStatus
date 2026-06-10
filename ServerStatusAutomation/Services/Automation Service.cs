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
            List<EventModel> pcStatuses = await _APIService.GetServerEvents("PC");
            List<EventModel> serverStatuses = await _APIService.GetServerEvents("Server");
            List<EventModel> connectionStatuses = await _APIService.GetServerEvents("Connection");
            AlertInformationModel? alerts = await _APIService.GetAlerts(1);

            foreach (ServerModel server in servers)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Checking Status for {server.HostName} - {server.Game} ({server.GameVersion})");

                EventModel? pcStatus = pcStatuses.Find(c => c.Server.Id == server.Id);
                EventModel? serverStatus = serverStatuses.Find(c => c.Server.Id == server.Id);
                EventModel? connectionStatus = connectionStatuses.Find(c => c.Server.Id == server.Id);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current PC Status: {pcStatus?.Status ?? StandardValues.MissingValues.Status}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current Connection Status: {connectionStatus?.Status ?? StandardValues.MissingValues.Status}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Current Server Status: {serverStatus?.Status ?? StandardValues.MissingValues.Status}");

                DateTime now = _Clock.UtcNow;
                DateTime refreshPeriod = now.AddMinutes(-SharedSettings.RefreshTime);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Refresh Period: {refreshPeriod} -> {now}");

                DateTime? downtime = null;

                if (server.Downtime != null)
                {
                    TimeSpan time = TimeSpan.Parse(server.Downtime.Time);
                    downtime = _Clock.UtcNow.Date.Add(time);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Downtime Period: {downtime} -> {downtime.Value.AddMinutes(10)}");
                }

                if (pcStatus != null && (pcStatus.DateOccured < refreshPeriod || pcStatus.Status != "Online"))
                {
                    if (pcStatus.Status != "Unknown" && (pcStatus.Status == "Online" || pcStatus.DateOccured < refreshPeriod))
                    {
                        pcStatus.Status = "Unknown";

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            "Updated PC Status to Unknown");
                    }

                    if (downtime == null || (pcStatus.DateOccured < downtime || pcStatus.DateOccured > downtime.Value.AddMinutes(10)))
                    {
                        await AlertsHandler(
                            alerts.Entries,
                            server,
                            pcStatus.Component,
                            pcStatus.Status);
                    }

                    if (pcStatus.DateOccured < refreshPeriod)
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = pcStatus.Component,
                            Status = pcStatus.Status,
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

                if (serverStatus != null && (serverStatus.DateOccured < refreshPeriod || serverStatus.Status != "Online"))
                {
                    if (serverStatus.Status != "Unknown" && (serverStatus.Status == "Online" || serverStatus.DateOccured < refreshPeriod))
                    {
                        serverStatus.Status = "Unknown";

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            "Updated Server Status to Unknown");
                    }

                    if (downtime == null || (serverStatus.DateOccured < downtime || serverStatus.DateOccured > downtime.Value.AddMinutes(10)))
                    {
                        await AlertsHandler(
                            alerts.Entries,
                            server,
                            serverStatus.Component,
                            serverStatus.Status);
                    }

                    if (serverStatus.DateOccured < refreshPeriod)
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = serverStatus.Component,
                            Status = serverStatus.Status,
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

                if (connectionStatus != null && (connectionStatus.DateOccured < refreshPeriod || connectionStatus.Status != "Online"))
                {
                    if (connectionStatus.Status != "Unknown" && (connectionStatus.Status == "Online" || connectionStatus.DateOccured < refreshPeriod))
                    {
                        connectionStatus.Status = "Unknown";

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            "Updated Connection Status to Unknown");
                    }

                    if (downtime == null || (connectionStatus.DateOccured < downtime || connectionStatus.DateOccured > downtime.Value.AddMinutes(10)))
                    {
                        await AlertsHandler(
                            alerts.Entries,
                            server,
                            connectionStatus.Component,
                            connectionStatus.Status);
                    }

                    if (connectionStatus.DateOccured < refreshPeriod)
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = connectionStatus.Component,
                            Status = connectionStatus.Status,
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

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Checked Status for {server.HostName} - {server.Game} ({server.GameVersion})");
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
                        }

                        await _discordService.SendNotification(
                            SharedSettings.RecipientId,
                            $"Automation has reported an issue with the {server.Game} ({server.GameVersion}) server. {component}: {status}");
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
                }

                await _discordService.SendNotification(
                    SharedSettings.RecipientId,
                    $"Automation has reported an issue with the {server.Game} ({server.GameVersion}) server. {component}: {status}");
            }
        }
    }
}
