// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Values;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusReporter.Abstractions;
using ServerStatusReporter.Models;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerStatusReporter.Services
{
    public class ApplicationService
    {
        private readonly ILoggerService _Logger;
        private readonly IClock _Clock;
        private readonly ITCPClient _TCPClient;
        private readonly IProcessService _ProcessService;
        private readonly APIService _APIService;
        private readonly PidFileService _PidFileService;

        private readonly Dictionary<int, Timer> _ServerTimers = [];
        private readonly Dictionary<int, DateTime> _ServerNextElapse = [];

        // Sets the class's global variables.
        public ApplicationService(
            ILoggerService _logger,
            IClock _clock,
            ITCPClient _tcpClient,
            IProcessService _processService,
            APIService _apiService,
            PidFileService pidFileService,
            SharedSettingsModel sharedSettings)
        {
            _Logger = _logger;
            _Clock = _clock;
            _TCPClient = _tcpClient;
            _ProcessService = _processService;
            _APIService = _apiService;
            _PidFileService = pidFileService;
        }

        /// <summary>
        /// Configures the API service logger.
        /// </summary>
        public void Setup()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Application Service");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configured Application Service");
        }

        /// <summary>
        /// Performs the first run for each server and starts per-server timers.
        /// </summary>
        public async Task Start()
        {
            List<ServerModel> servers = await _APIService.GetServers();

            for (int i = 0; i < AppSettingsModel.Servers.Length; i++)
            {
                ServerModel? server = servers.Find(c => c.Name == AppSettingsModel.Servers[i]);

                if (server != null)
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

                else
                {
                    _Logger.LogMessage(
                       StandardValues.LoggerValues.Info,
                       $"No Server Found for {AppSettingsModel.Servers[i]}");
                }
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
                $"Running Event Register for {server.Name}");

            Dictionary<string, List<EventModel>> componentStatuses = [];

            foreach (string component in AppSettingsModel.Components)
            {
                componentStatuses[component] = await _APIService.GetServerEvents(component);
            }

            if (server.Downtime != null)
            {
                TimeSpan downtimeTime = TimeSpan.Parse(server.Downtime.Time);
                DateTime downtimeStart = DateTime.SpecifyKind(
                    runStartTime.Date.Add(downtimeTime),
                    DateTimeKind.Utc);
                DateTime downtimeEnd = downtimeStart.AddSeconds(server.Downtime.Duration);

                if (runStartTime >= downtimeStart && runStartTime <= downtimeEnd)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Skipping event registration for {server.Name} - server in downtime");

                    return;
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering Events for {server.Name}");

            foreach (string component in AppSettingsModel.Components)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Component: {component}");

                if (component == StandardValues.ComponentValues.PC)
                {
                    string determinedStatus = StandardValues.StatusValues.Online;

                    if (!ShouldSkipRegistration(
                        componentStatuses,
                        StandardValues.ComponentValues.PC,
                        server.Id,
                        server.EventInterval,
                        runStartTime))
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = StandardValues.ComponentValues.PC,
                            Status = determinedStatus,
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

                if (component == StandardValues.ComponentValues.Server)
                {
                    string determinedStatus = await ServerRunning(server.Name)
                        ? StandardValues.StatusValues.Online
                        : StandardValues.StatusValues.Offline;

                    if (!ShouldSkipRegistration(
                        componentStatuses,
                        StandardValues.ComponentValues.Server,
                        server.Id,
                        server.EventInterval,
                        runStartTime))
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = StandardValues.ComponentValues.Server,
                            Status = determinedStatus,
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

                if (component == StandardValues.ComponentValues.Connection)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"IP Address: {server.Connection.IPAddress}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Port: {server.Connection.Port}");

                    string pingStatus = await PingAddress(
                        server.Connection.IPAddress,
                        server.Connection.Port);

                    string determinedStatus = pingStatus switch
                    {
                        "Success" => StandardValues.StatusValues.Online,
                        "Failed" => StandardValues.StatusValues.Offline,
                        _ => StandardValues.StatusValues.Unknown
                    };

                    if (!ShouldSkipRegistration(
                        componentStatuses,
                        StandardValues.ComponentValues.Connection,
                        server.Id,
                        server.EventInterval,
                        runStartTime))
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = StandardValues.ComponentValues.Connection,
                            Status = determinedStatus,
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
                $"Registered Events for {server.Name}");
        }

        /// <summary>
        /// Tries to ping a given IP address.
        /// </summary>
        private async Task<string> PingAddress(
            string ipAddress,
            int port)
        {
            string response;

            bool success = await _TCPClient.PingAddress(
                ipAddress,
                port);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Connection Status: {success}");

            if (success)
            {
                response = "Success";
            }

            else
            {
                response = "Failed";
            }

            return response;
        }

        /// <summary>
        /// Determines whether to skip registering an event based on existing events.
        /// </summary>
        private bool ShouldSkipRegistration(
            Dictionary<string, List<EventModel>> componentStatuses,
            string component,
            int serverId,
            int eventInterval,
            DateTime runStartTime)
        {
            bool skipRegistration = false;

            if (componentStatuses.TryGetValue(
                component,
                out List<EventModel>? statuses))
            {
                EventModel? existingEvent = statuses.Find(c => c.Server.Id == serverId);

                if (existingEvent != null)
                {
                    DateTime refreshPeriod = runStartTime.AddSeconds(-eventInterval);

                    if (existingEvent.DateOccured > refreshPeriod)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Skipping {component} event registration - recent event exists");

                        skipRegistration = true;
                    }
                }
            }

            return skipRegistration;
        }

        /// <summary>
        /// Checks if a server is running by reading its PID file and verifying the process.
        /// </summary>
        private async Task<bool> ServerRunning(string serverName)
        {
            bool running = false;

            (int processId, DateTime expectedStartTime)? pidData = await _PidFileService.Read(serverName);

            if (pidData == null)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Server Running: False");
            }

            else
            {
                running = _ProcessService.IsRunning(
                    pidData.Value.processId,
                    pidData.Value.expectedStartTime);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Server Running: {running}");
            }

            return running;
        }
    }
}
