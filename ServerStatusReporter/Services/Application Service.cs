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
        private readonly SharedSettingsModel SharedSettings;

        private Timer RefreshTimer;
        private DateTime NextElapse;

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
            SharedSettings = sharedSettings;
        }

        /// <summary>
        /// Configures the timer and API service logger.
        /// </summary>
        public void Setup()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Application Service");

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
                "Configured Application Service");
        }

        /// <summary>
        /// Performs the first run and starts the timer.
        /// </summary>
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

        /// <summary>
        /// Performs a run then restarts the timer.
        /// </summary>
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
                    $"Token Expiry: {_APIService.ExpiryTime}");
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
            DateTime runStartTime = _Clock.UtcNow;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Running Event Register");

            List<ServerModel> servers = await _APIService.GetServers();

            Dictionary<string, List<EventModel>> componentStatuses = [];

            foreach (string component in AppSettingsModel.Components)
            {
                componentStatuses[component] = await _APIService.GetServerEvents(component);
            }

            for (int i = 0; i < AppSettingsModel.Servers.Length; i++)
            {
                ServerModel? server = servers.Find(c => c.Name == AppSettingsModel.Servers[i]);

                if (server != null)
                {
                    if (server.Downtime != null)
                    {
                        DateTime now = _Clock.UtcNow;

                        TimeSpan downtimeTime = TimeSpan.Parse(server.Downtime.Time);
                        DateTime downtimeStart = DateTime.SpecifyKind(
                            now.Date.Add(downtimeTime),
                            DateTimeKind.Utc);
                        DateTime downtimeEnd = downtimeStart.AddSeconds(server.Downtime.Duration);

                        if (now >= downtimeStart && now <= downtimeEnd)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Info,
                                $"Skipping event registration for {server.Name} - server in downtime");

                            continue;
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
                        $"Registered Events for {server.Name}");
                }

                else
                {
                    _Logger.LogMessage(
                       StandardValues.LoggerValues.Info,
                       $"No Server Found for {AppSettingsModel.Servers[i]}");
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Ran Event Register");
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
