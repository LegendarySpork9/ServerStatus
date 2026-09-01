// Copyright © - Unpublished - Toby Hunter
using Newtonsoft.Json;
using RestSharp;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Models;
using ServerStatusSite.Models.Requests;
using ServerStatusSite.Models.Responses;
using System.Text;

namespace ServerStatusSite.Implementations
{
    public class BackupToolAPIClientWrapper : IBackupToolAPIClient
    {
        private readonly ILoggerService _Logger;
        private readonly IRestClientWrapper _RestClient;
        private readonly BackupToolSettingsModel Settings;

        // Sets the class's global variables.
        public BackupToolAPIClientWrapper(
            ILoggerService _logger,
            IRestClientWrapper _restClient,
            BackupToolSettingsModel settings)
        {
            _Logger = _logger;
            _RestClient = _restClient;
            Settings = settings;
        }

        /// <summary>
        /// Returns logs from the Backup Tool API.
        /// </summary>
        public async Task<(LogsResponseModel?, bool)> GetLogs(
            string serverName,
            List<KeyValuePair<string, object>> queryParameters)
        {
            LogsResponseModel? logs = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/logs",
                    queryParameters: queryParameters);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    RestRequest request = new()
                    {
                        Method = Method.Get
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Rest Request");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Sending Request");

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                    {
                        logs = JsonConvert.DeserializeObject<LogsResponseModel>(response.Content);

                        if (logs != null)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Logs Returned: {logs.Logs.Count}");

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
                logs,
                success);
        }

        /// <summary>
        /// Returns the list of archived log files from the Backup Tool API.
        /// </summary>
        public async Task<(LogArchivesResponseModel?, bool)> GetLogArchives(string serverName)
        {
            LogArchivesResponseModel? archives = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/logs/archived");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    RestRequest request = new()
                    {
                        Method = Method.Get
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Rest Request");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Sending Request");

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                    {
                        archives = JsonConvert.DeserializeObject<LogArchivesResponseModel>(response.Content);

                        if (archives != null)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Archives Returned: {archives.Archives.Count}");

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
                archives,
                success);
        }

        /// <summary>
        /// Returns the logs from a specific archive file from the Backup Tool API.
        /// </summary>
        public async Task<(ArchivedLogsResponseModel?, bool)> GetArchivedLogs(
            string serverName,
            string fileName)
        {
            ArchivedLogsResponseModel? archivedLogs = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/logs/archived",
                    entityId: fileName);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    RestRequest request = new()
                    {
                        Method = Method.Get
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Rest Request");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Sending Request");

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
                    {
                        archivedLogs = JsonConvert.DeserializeObject<ArchivedLogsResponseModel>(response.Content);

                        if (archivedLogs != null)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Archived Logs Returned: {archivedLogs.Logs.Sum(l => l.Content.Count)}");

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
                archivedLogs,
                success);
        }

        /// <summary>
        /// Registers a webhook with the Backup Tool API.
        /// </summary>
        public async Task<(WebhookRegistrationResponseModel?, bool)> RegisterWebhook(
            string serverName,
            string body)
        {
            WebhookRegistrationResponseModel? registration = null;
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/webhooks");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    RestRequest request = new()
                    {
                        Method = Method.Post
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);
                    request.AddHeader(
                        "Accept",
                        "application/json");
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

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Created && response.Content != null)
                    {
                        registration = JsonConvert.DeserializeObject<WebhookRegistrationResponseModel>(response.Content);

                        if (registration != null)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Webhook Registered: {registration.Id}");

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
                registration,
                success);
        }

        /// <summary>
        /// Unregisters a webhook from the Backup Tool API.
        /// </summary>
        public async Task<(bool, bool)> UnregisterWebhook(
            string serverName,
            string webhookId)
        {
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/webhooks",
                    entityId: webhookId);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    RestRequest request = new()
                    {
                        Method = Method.Delete
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Rest Request");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Sending Request");

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        success = true;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Webhook Unregistered: {webhookId}");
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
                success,
                success);
        }

        /// <summary>
        /// Sends a command to the Backup Tool API.
        /// </summary>
        public async Task<(bool, bool)> SendCommand(
            string serverName,
            CommandRequestModel command)
        {
            bool success = false;

            try
            {
                string url = URLBuilderFunction.BuildURL(
                    GetBaseUrl(serverName),
                    "/commands");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"URL: {url}");

                string? credentials = GetCredentials(serverName);

                if (credentials != null)
                {

                    string body = JsonConvert.SerializeObject(command);

                    RestRequest request = new()
                    {
                        Method = Method.Post
                    };
                    request.AddHeader(
                        "Authorization",
                        credentials);
                    request.AddHeader(
                        "Accept",
                        "application/json");
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

                    RestResponse response = await _RestClient.ExecuteAsync(
                        url,
                        request);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Code: {response.StatusCode}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Response Message: {response.Content ?? "No Response Content"}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
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
                success,
                success);
        }

        /// <summary>
        /// Returns the base URL for the given server's Backup Tool API.
        /// </summary>
        private string GetBaseUrl(string serverName) => string.Format(
            Settings.APIURLTemplate,
            serverName);

        /// <summary>
        /// Returns the Basic Auth credentials for the given server.
        /// </summary>
        private string? GetCredentials(string serverName)
        {
            string? credentials = null;

            if (Settings.Servers.TryGetValue(
                serverName,
                out ServerCredentialsModel? serverCredentials))
            {
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{serverCredentials.ClientId}:{serverCredentials.ClientSecret}"));
                credentials = $"Basic {encoded}";
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"No credentials configured for server: {serverName}");
            }

            return credentials;
        }
    }
}
