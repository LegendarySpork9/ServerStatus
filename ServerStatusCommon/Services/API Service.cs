// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Services
{
    public class APIService
    {
        private ILoggerService _Logger;
        private readonly IAPIClient _APIClient;
        private readonly IClock _Clock;
        
        public DateTime ExpiryTime { get; set; }
        private int RetryCount { get; set; } = 0;

        // Sets the class's global variables.
        public APIService(
            ILoggerService _logger,
            IAPIClient _apiClient,
            IClock _clock)
        {
            _Logger = _logger;
            _APIClient = _apiClient;
            _Clock = _clock;
        }

        /// <summary>
        /// Sets the logger.
        /// </summary>
        public void SetLogger(ILoggerService _logger)
        {
            _Logger = _logger;
        }

        /// <summary>
        /// Gets a bearer token from the API.
        /// </summary>
        public async Task Authorise()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Obtaining Bearer token from API");

            try
            {
                AuthenticationModel? auth = await _APIClient.Authorise();

                if (auth != null)
                {
                    _APIClient.SetBearerToken(auth.Token);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Bearer Token: {auth.Token}");

                    ExpiryTime = DateTime.SpecifyKind(
                        auth.Info.Expires,
                        DateTimeKind.Utc);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Expiry Time: {ExpiryTime}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Obtained Bearer Token from API");
                }

                if (auth == null)
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await _APIClient.Authorise();
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch users from API");
                    }
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
                StandardValues.LoggerValues.Info,
                "Obtained Bearer token from API");
        }

        /// <summary>
        /// Gets a list of the users from the API.
        /// </summary>
        public async Task<List<UserModel>> GetUsers()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Fetching users from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            List<UserModel> users = [];

            try
            {
                (users, bool success) = await _APIClient.GetUsers();

                if (success)
                {
                    foreach (UserModel user in users)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"User Id: {user.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"User Username: {user.Username}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"User Password: {user.Password}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"User Scopes: {string.Join(
                                ", ",
                                user.Scopes)}");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Fetched users from API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        users = await GetUsers();
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch users from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Failed to fetch users from API");
            }

            RetryCount = 0;
            return users;
        }

        /// <summary>
        /// Gets the user settings for the given user.
        /// </summary>
        public async Task<UserModel> GetUserSettings(UserModel user)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Fetching user settings from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            try
            {
                (List<UserSettingModel> userSettings, bool success) = await _APIClient.GetUserSettings(user.Id);

                if (success)
                {
                    if (userSettings.Count > 0)
                    {
                        user.Settings = userSettings[0].Settings;

                        foreach (SettingModel setting in userSettings[0].Settings)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Setting Id: {setting.Id}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Setting Name: {setting.Name}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Setting Value: {setting.Value}");
                        }

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Fetched user settings from API");
                    }
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        user = await GetUserSettings(user);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch user settings from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Failed to fetch user settings from API");
            }

            RetryCount = 0;
            return user;
        }

        /// <summary>
        /// Gets a list of active servers.
        /// </summary>
        public async Task<List<ServerModel>> GetServers()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Fetching servers from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            List<ServerModel> servers = [];

            try
            {
                (servers, bool success) = await _APIClient.GetServers();

                if (success)
                {
                    foreach (ServerModel server in servers)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Id: {server.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Name: {server.Name}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Host Name: {server.HostName}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game: {server.Game}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game Version: {server.GameVersion}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Connection: {server.Connection.IPAddress}:{server.Connection.Port}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Downtime: {server.Downtime?.Time ?? "No Downtime"}");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Fetched servers from API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        servers = await GetServers();
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch servers from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Failed to fetch servers from API");
            }

            RetryCount = 0;
            return servers;
        }

        /// <summary>
        /// Gets a ist of the statuses for a given component.
        /// </summary>
        public async Task<List<EventModel>> GetServerEvents(string component)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Fetching server events from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            List<EventModel> serverEvents = [];

            List<KeyValuePair<string, object>> queryParameters =
            [
                new("component", component)
            ];

            try
            {
                (serverEvents, bool success) = await _APIClient.GetServerEvents(queryParameters);

                if (success)
                {
                    foreach (EventModel serverEvent in serverEvents)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Converting date times to UTC for server event {serverEvent.Id}.");

                        serverEvent.DateOccured = DateTime.SpecifyKind(
                            serverEvent.DateOccured,
                            DateTimeKind.Utc);

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Converted date times to UTC for server event {serverEvent.Id}.");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Event Id: {serverEvent.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Component: {serverEvent.Component}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Status: {serverEvent.Status}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Occured: {serverEvent.DateOccured}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Id: {serverEvent.Server.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Name: {serverEvent.Server.Name}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Host Name: {serverEvent.Server.HostName}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game: {serverEvent.Server.Game}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game Version: {serverEvent.Server.GameVersion}");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Fetched server statuses from API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        serverEvents = await GetServerEvents(component);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch server events from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Failed to fetch server events from API");
            }

            RetryCount = 0;
            return serverEvents;
        }

        /// <summary>
        /// Changes the value of the given user setting.
        /// </summary>
        public async Task<(SettingModel?, ResponseModel?)> UpdateUserSettings(
            int userSettingId,
            UserSettingUpdateRequestModel userSetting)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Updating user setting, {userSettingId}, in API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            SettingModel? updatedSetting = null;
            ResponseModel? apiResponse = null;

            try
            {
                (updatedSetting, apiResponse) = await _APIClient.UpdateUserSettings(
                    userSettingId,
                    userSetting);

                if (updatedSetting != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Setting Id: {updatedSetting.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Setting Name: {updatedSetting.Name}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Setting Value: {updatedSetting.Value}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Updated user setting, {userSettingId}, in API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        (updatedSetting, apiResponse) = await UpdateUserSettings(
                            userSettingId,
                            userSetting);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to update setting, {userSettingId}, in API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to update setting, {userSettingId}, in API");
            }

            RetryCount = 0;
            return (
                updatedSetting,
                apiResponse);
        }

        /// <summary>
        /// Changes the information of a given user.
        /// </summary>
        public async Task<(UserModel?, ResponseModel?)> UpdateUser(
            int userId,
            UserUpdateRequestModel user)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Updating user, {userId}, in API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            UserModel? updatedUser = null;
            ResponseModel? apiResponse = null;

            try
            {
                (updatedUser, apiResponse) = await _APIClient.UpdateUser(
                    userId,
                    user);

                if (updatedUser != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"User Id: {updatedUser.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"User Username: {updatedUser.Username}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"User Password: {updatedUser.Password}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"User Scopes: {string.Join(
                            ", ",
                            updatedUser.Scopes)}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Updated user, {userId}, in API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        (updatedUser, apiResponse) = await UpdateUser(
                            userId,
                            user);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to update user, {userId}, in API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to update user, {userId}, in API");
            }

            RetryCount = 0;
            return (updatedUser, apiResponse);
        }

        /// <summary>
        /// Gets the alerts on a given page.
        /// </summary>
        public async Task<AlertInformationModel?> GetAlerts(int pageNumber)
        {
            _Logger.LogMessage(StandardValues.LoggerValues.Info, "Fetching alerts from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            AlertInformationModel? alerts = null;
            bool success;

            List<KeyValuePair<string, object>> queryParameters =
            [
                new("pageNumber", pageNumber)
            ];

            try
            {
                (alerts, success) = await _APIClient.GetAlerts(queryParameters);

                if (success)
                {
                    if (alerts != null)
                    {
                        foreach (AlertModel alert in alerts.Entries)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Info,
                                $"Converting date times to UTC for alert {alert.Id}.");

                            alert.AlertDate = DateTime.SpecifyKind(
                                alert.AlertDate,
                                DateTimeKind.Utc);

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Info,
                                $"Converted date times to UTC for alert {alert.Id}.");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Alert Id: {alert.Id}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Reporter: {alert.Reporter}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Component: {alert.Component}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Component Status: {alert.ComponentStatus}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Alert Status: {alert.AlertStatus}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Alert Date: {alert.AlertDate}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Server Id: {alert.Server.Id}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Server Name: {alert.Server.Name}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Host Name: {alert.Server.HostName}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Game: {alert.Server.Game}");
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Game Version: {alert.Server.GameVersion}");
                        }

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Fetched alerts from API");
                    }
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        alerts = await GetAlerts(pageNumber);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Failed to fetch alerts from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Failed to fetch alerts from API");
            }

            RetryCount = 0;
            return alerts;
        }

        /// <summary>
        /// Gets the information of a given AlertID.
        /// </summary>
        public async Task<AlertModel?> GetAlert(int alertId)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching alert, {alertId}, from API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            AlertModel? alert = null;
            bool success;

            try
            {
                (alert, success) = await _APIClient.GetAlert(alertId);

                if (success)
                {
                    if (alert != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Converting date times to UTC for alert {alert.Id}.");

                        alert.AlertDate = DateTime.SpecifyKind(
                            alert.AlertDate,
                            DateTimeKind.Utc);

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Converted date times to UTC for alert {alert.Id}.");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Alert Id: {alert.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Reporter: {alert.Reporter}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Component: {alert.Component}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Component Status: {alert.ComponentStatus}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Alert Status: {alert.AlertStatus}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Alert Date: {alert.AlertDate}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Id: {alert.Server.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Server Name: {alert.Server.Name}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Host Name: {alert.Server.HostName}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game: {alert.Server.Game}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Game Version: {alert.Server.GameVersion}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Fetched alert, {alertId}, from API");
                    }
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        alert = await GetAlert(alertId);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to fetch alert, {alertId}, from API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to fetch alert, {alertId}, from API");
            }

            RetryCount = 0;
            return alert;
        }

        /// <summary>
        /// Changes the status of the given alert.
        /// </summary>
        public async Task<(AlertModel?, ResponseModel?)> UpdateAlert(
            int alertId,
            AlertUpdateRequestModel alert)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Updating alert, {alertId}, in API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            AlertModel? updatedAlert = null;
            ResponseModel? apiResponse = null;

            try
            {
                (updatedAlert, apiResponse) = await _APIClient.UpdateAlert(
                    alertId,
                    alert);

                if (updatedAlert != null)
                {
                    _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Converting date times to UTC for alert {updatedAlert.Id}.");

                    updatedAlert.AlertDate = DateTime.SpecifyKind(
                        updatedAlert.AlertDate,
                        DateTimeKind.Utc);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Converted date times to UTC for alert {updatedAlert.Id}.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Id: {updatedAlert.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Reporter: {updatedAlert.Reporter}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component: {updatedAlert.Component}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component Status: {updatedAlert.ComponentStatus}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Status: {updatedAlert.AlertStatus}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Date: {updatedAlert.AlertDate}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Id: {updatedAlert.Server.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {updatedAlert.Server.Name}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Host Name: {updatedAlert.Server.HostName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game: {updatedAlert.Server.Game}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game Version: {updatedAlert.Server.GameVersion}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Updated alert, {alertId}, in API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        (updatedAlert, apiResponse) = await UpdateAlert(
                            alertId,
                            alert);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to update alert, {alertId}, in API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to update alert, {alertId}, in API");
            }

            RetryCount = 0;
            return (
                updatedAlert,
                apiResponse);
        }

        /// <summary>
        /// Adds a new alert to the API.
        /// </summary>
        public async Task<(AlertModel?, ResponseModel?)> RegisterAlert(AlertRequestModel alert)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering alert, {alert.Component} ({alert.ComponentStatus}), in API");

            if (ExpiryTime < _Clock.UtcNow)
            {
                await Authorise();
            }

            AlertModel? createdAlert = null;
            ResponseModel? apiResponse = null;

            try
            {
                (createdAlert, apiResponse) = await _APIClient.RegisterAlert(alert);

                if (createdAlert != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Converting date times to UTC for alert {createdAlert.Id}.");

                    createdAlert.AlertDate = DateTime.SpecifyKind(
                        createdAlert.AlertDate,
                        DateTimeKind.Utc);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Converted date times to UTC for alert {createdAlert.Id}.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Id: {createdAlert.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Reporter: {createdAlert.Reporter}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component: {createdAlert.Component}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component Status: {createdAlert.ComponentStatus}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Status: {createdAlert.AlertStatus}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Alert Date: {createdAlert.AlertDate}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Id: {createdAlert.Server.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {createdAlert.Server.Name}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Host Name: {createdAlert.Server.HostName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game: {createdAlert.Server.Game}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game Version: {createdAlert.Server.GameVersion}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Registered alert, {alert.Component} ({alert.ComponentStatus}), in API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        (createdAlert, apiResponse) = await RegisterAlert(alert);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to register alert, {alert.Component} ({alert.ComponentStatus}), in API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to register alert, {alert.Component} ({alert.ComponentStatus}), in API");
            }

            RetryCount = 0;
            return (
                createdAlert,
                apiResponse);
        }

        /// <summary>
        /// Adds a new event to the API.
        /// </summary>
        public async Task<(EventModel?, ResponseModel?)> RegisterServerEvent(EventRequestModel serverEvent)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering event, {serverEvent.Component} ({serverEvent.Status}), in API");

            if (ExpiryTime <= _Clock.UtcNow)
            {
                await Authorise();
            }

            EventModel? createdEvent = null;
            ResponseModel? apiResponse = null;

            try
            {
                (createdEvent, apiResponse) = await _APIClient.RegisterServerEvent(serverEvent);

                if (createdEvent != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Converting date times to UTC for server event {createdEvent.Id}.");

                    createdEvent.DateOccured = DateTime.SpecifyKind(
                        createdEvent.DateOccured,
                        DateTimeKind.Utc);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Converted date times to UTC for server event {createdEvent.Id}.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Event Id: {createdEvent.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Component: {createdEvent.Component}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Status: {createdEvent.Status}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Occured: {createdEvent.DateOccured}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Id: {createdEvent.Server.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {createdEvent.Server.Name}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Host Name: {createdEvent.Server.HostName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game: {createdEvent.Server.Game}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Game Version: {createdEvent.Server.GameVersion}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Registered event, {serverEvent.Component} ({serverEvent.Status}), in API");
                }

                else
                {
                    if (RetryCount != 4)
                    {
                        RetryCount++;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Retry {RetryCount} of 4");

                        await Authorise();
                        (createdEvent, apiResponse) = await RegisterServerEvent(serverEvent);
                    }

                    else
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Failed to register event, {serverEvent.Component} ({serverEvent.Status}), in API");
                    }
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
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Failed to register event, {serverEvent.Component} ({serverEvent.Status}), in API");
            }

            RetryCount = 0;
            return (
                createdEvent,
                apiResponse);
        }
    }
}
