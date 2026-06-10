// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusReporter.Abstractions;
using ServerStatusReporter.Models;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerStatusReporter.Services
{
    public class ApplicationService
    {
        private readonly ILoggerService _Logger;
        private readonly IClock _Clock;
        private readonly ITCPClient _TCPClient;
        private readonly APIService _APIService;
        private readonly SharedSettingsModel SharedSettings;

        private Timer RefreshTimer;
        private DateTime NextElapse;

        // Sets the class's global variables.
        public ApplicationService(
            ILoggerService _logger,
            IClock _clock,
            ITCPClient _tcpClient,
            APIService _apiService,
            SharedSettingsModel sharedSettings)
        {
            _Logger = _logger;
            _Clock = _clock;
            _TCPClient = _tcpClient;
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
                    x.Message);
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
                "Running Event Register");

            List<ServerModel> servers = await _APIService.GetServers();

            for (int i = 0; i < AppSettingsModel.Games.Length; i++)
            {
                string[] gameParts = AppSettingsModel.Games[i].Split('_');
                gameParts[1] = gameParts[1].Replace("(", "").Replace(")", "");

                ServerModel? server = servers.Find(c => c.HostName == AppSettingsModel.HostName && c.Game == gameParts[0] && c.GameVersion == gameParts[1]);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Registering Events for {server.HostName ?? StandardValues.MissingValues.HostName} - {server.Game ?? StandardValues.MissingValues.Game} ({server.GameVersion ?? StandardValues.MissingValues.GameVersion})");

                foreach (string component in AppSettingsModel.Components)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component: {component}");

                    if (component == "PC")
                    {
                        EventRequestModel newEvent = new()
                        {
                            Component = "PC",
                            Status = "Online",
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

                    if (component == "Server")
                    {
                        if (ServerRunning(AppSettingsModel.ServerPaths[i]))
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = "Server",
                                Status = "Online",
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

                        else
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = "PC",
                                Status = "Offline",
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

                    if (component == "Connection")
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"IP Address: {server.Connection.IPAddress}");

                        string pingStatus = await PingAddress(
                            server.Connection.IPAddress,
                            server.Connection.Port);

                        if (pingStatus == "Success")
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = "Connection",
                                Status = "Online",
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

                        else if (pingStatus == "Failed")
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = "Connection",
                                Status = "Offline",
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

                        else
                        {
                            EventRequestModel newEvent = new()
                            {
                                Component = "Connection",
                                Status = "Unknown",
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
                    $"Registered Events for {server.HostName} - {server.Game} ({server.GameVersion})");
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
            string response = string.Empty;

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
        /// Checks if a process is running from the given path.
        /// </summary>
        private bool ServerRunning(string serverPath)
        {
            bool running = false;

            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainModule != null && process.MainModule.FileName.StartsWith(
                            serverPath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            running = true;
                            process.Dispose();
                            break;
                        }
                    }

                    catch
                    {
                        
                    }

                    process.Dispose();
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

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Server Running: {running}");
            return running;
        }
    }
}
