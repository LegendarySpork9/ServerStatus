// Copyright © - Unpublished - Toby Hunter
using Newtonsoft.Json;
using RestSharp;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Implementations
{
    public class APIClientWrapper : IAPIClient
    {
        private readonly ILoggerService _Logger;
        private readonly IFileSystem _FileSystem;
        private readonly SharedSettingsModel SharedSettings;

        private string? BearerToken;

        // Sets the class's global variables.
        public APIClientWrapper(
            ILoggerService _logger,
            IFileSystem _fileSystem,
            SharedSettingsModel sharedSettings)
        {
            _Logger = _logger;
            _FileSystem = _fileSystem;
            SharedSettings = sharedSettings;
        }

        /// <summary>
        /// Sets the bearer token to the given value.
        /// </summary>
        public void SetBearerToken(string bearerToken) => BearerToken = bearerToken;

        /// <summary>
        /// Returns the authentication from the API.
        /// </summary>
        public async Task<(AuthenticationModel?, ResponseModel?)> Authorise()
        {
            AuthenticationModel? auth = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/auth/token");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    SharedSettings.Credentials);
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = await _FileSystem.ReadAllText(SharedSettings.AuthPayloadLocation);

                RestRequest request = new()
                {
                    Method = Method.Post
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    auth = JsonConvert.DeserializeObject<AuthenticationModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    apiResponse = new()
                    {
                        StatusCode = System.Net.HttpStatusCode.ServiceUnavailable,
                        Message = response.ErrorException.Message
                    };

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (auth, apiResponse);
        }

        /// <summary>
        /// Returns a list of users from the API.
        /// </summary>
        public async Task<(PagedResponseModel<UserModel>?, bool)> GetUsers(List<KeyValuePair<string, object>> queryParameters)
        {
            PagedResponseModel<UserModel>? users = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/user",
                    queryParameters: queryParameters);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    users = JsonConvert.DeserializeObject<PagedResponseModel<UserModel>>(response.Content);

                    if (users != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Users Returned: {users.EntryCount}");

                        success = true;
                    }
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                users,
                success);
        }

        /// <summary>
        /// Returns a list of user settings from the API for a given user.
        /// </summary>
        public async Task<(List<UserSettingModel>, bool)> GetUserSettings(int userId)
        {
            List<UserSettingModel> userSettings = [];
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/usersettings",
                    entityId: userId);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    userSettings = JsonConvert.DeserializeObject<List<UserSettingModel>>(response.Content) ?? [];

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"User Settings Returned: {userSettings.Select(us => us.Settings.Count).Sum()}");

                    success = true;
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                userSettings,
                success);
        }

        /// <summary>
        /// Returns a list of servers from the API.
        /// </summary>
        public async Task<(PagedResponseModel<ServerModel>?, bool)> GetServers(List<KeyValuePair<string, object>> queryParameters)
        {
            PagedResponseModel<ServerModel>? servers = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serverinformation",
                    queryParameters: queryParameters);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    servers = JsonConvert.DeserializeObject<PagedResponseModel<ServerModel>>(response.Content);

                    if (servers != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Servers Returned: {servers.EntryCount}");

                        success = true;
                    }
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                servers,
                success);
        }

        /// <summary>
        /// Returns a list of server status from the API for a given component.
        /// </summary>
        public async Task<(List<EventModel>, bool)> GetServerEvents(List<KeyValuePair<string, object>> queryParameters)
        {
            List<EventModel> serverEvents = [];
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serverevent",
                    queryParameters: queryParameters);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    serverEvents = JsonConvert.DeserializeObject<List<EventModel>>(response.Content) ?? [];

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Events Returned: {serverEvents.Count}");

                    success = true;
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                serverEvents,
                success);
        }

        /// <summary>
        /// Updates the given user setting in the API.
        /// </summary>
        public async Task<(SettingModel?, ResponseModel?)> UpdateUserSettings(
            int userSettingId,
            UserSettingUpdateRequestModel userSetting)
        {
            SettingModel? updatedSetting = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/usersettings",
                    entityId: userSettingId,
                    ignoreQuery: true);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = JsonConvert.SerializeObject(userSetting);

                RestRequest request = new()
                {
                    Method = Method.Patch
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    updatedSetting = JsonConvert.DeserializeObject<SettingModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                updatedSetting,
                apiResponse);
        }

        /// <summary>
        /// Updates the given user in the API.
        /// </summary>
        public async Task<(UserModel?, ResponseModel?)> UpdateUser(
            int userId,
            UserUpdateRequestModel user)
        {
            UserModel? updatedUser = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/user",
                    entityId: userId,
                    ignoreQuery: true);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = JsonConvert.SerializeObject(user);

                RestRequest request = new()
                {
                    Method = Method.Patch
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    updatedUser = JsonConvert.DeserializeObject<UserModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                updatedUser,
                apiResponse);
        }

        /// <summary>
        /// Returns a list of alerts from the API.
        /// </summary>
        public async Task<(PagedResponseModel<AlertModel>?, bool)> GetAlerts(List<KeyValuePair<string, object>> queryParameters)
        {
            PagedResponseModel<AlertModel>? alerts = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serveralert",
                    queryParameters: queryParameters);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    alerts = JsonConvert.DeserializeObject<PagedResponseModel<AlertModel>>(response.Content);

                    if (alerts != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Alerts Returned: {alerts.EntryCount}");

                        success = true;
                    }
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                alerts,
                success);
        }

        /// <summary>
        /// Returns a given alert from the API.
        /// </summary>
        public async Task<(AlertModel?, bool)> GetAlert(int alertId)
        {
            AlertModel? alert = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serveralert",
                    entityId: alertId);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    alert = JsonConvert.DeserializeObject<AlertModel>(response.Content);

                    if (alert != null)
                    {
                        success = true;
                    }
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                alert,
                success);
        }

        /// <summary>
        /// Updates the given alert in the API.
        /// </summary>
        public async Task<(AlertModel?, ResponseModel?)> UpdateAlert(
            int alertId,
            AlertUpdateRequestModel alert)
        {
            AlertModel? updatedAlert = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serveralert",
                    entityId: alertId);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = JsonConvert.SerializeObject(alert);

                RestRequest request = new()
                {
                    Method = Method.Patch
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    updatedAlert = JsonConvert.DeserializeObject<AlertModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                updatedAlert,
                apiResponse);
        }

        /// <summary>
        /// Adds the given alert to the API.
        /// </summary>
        public async Task<(AlertModel?, ResponseModel?)> RegisterAlert(AlertRequestModel alert)
        {
            AlertModel? createdAlert = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serveralert");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = JsonConvert.SerializeObject(alert);

                RestRequest request = new()
                {
                    Method = Method.Post
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.Created && response.Content != null)
                {
                    createdAlert = JsonConvert.DeserializeObject<AlertModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                createdAlert,
                apiResponse);
        }

        /// <summary>
        /// Adds the given event to the API.
        /// </summary>
        public async Task<(EventModel?, ResponseModel?)> RegisterServerEvent(EventRequestModel newEvent)
        {
            EventModel? createdEvent = null;
            ResponseModel? apiResponse = null;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/serverstatus/serverevent");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");
                client.AddDefaultHeader(
                    "Accept",
                    "application/json");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                string body = JsonConvert.SerializeObject(newEvent);

                RestRequest request = new()
                {
                    Method = Method.Post
                };
                request.AddParameter(
                    "application/json",
                    body,
                    ParameterType.RequestBody);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Request Body: {body}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.Created && response.Content != null)
                {
                    createdEvent = JsonConvert.DeserializeObject<EventModel>(response.Content);
                }

                else if (response.Content != null)
                {
                    APIMessageModel? apiMessage = JsonConvert.DeserializeObject<APIMessageModel>(response.Content);
                    apiResponse = new()
                    {
                        StatusCode = response.StatusCode,
                        Message = apiMessage?.Error ?? apiMessage?.Information ?? "No message returned by the API."
                    };
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                createdEvent,
                apiResponse);
        }

        /// <summary>
        /// Returns a list of components from the API.
        /// </summary>
        public async Task<(List<ComponentModel>, bool)> GetComponents()
        {
            List<ComponentModel> components = [];
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    SharedSettings.BaseURL,
                    "/configuration/component");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                RestClient client = new(url);
                client.AddDefaultHeader(
                    "Authorization",
                    $"Bearer {BearerToken}");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Client");

                RestRequest request = new()
                {
                    Method = Method.Get
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Rest Request");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                RestResponse response = await client.ExecuteAsync(request);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Code: {response.StatusCode}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Response Message: {response.Content ?? "No Response Content"}");

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                {
                    ComponentInformationModel? componentInfo = JsonConvert.DeserializeObject<ComponentInformationModel>(response.Content);

                    if (componentInfo != null)
                    {
                        components = componentInfo.Entries;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Components Returned: {components.Count}");

                        success = true;
                    }
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    success = true;
                }

                if (response.ErrorException != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Error: {response.ErrorException.Message}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Response Stack Trace: {response.ErrorException.StackTrace}");
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

            return (
                components,
                success);
        }
    }
}
