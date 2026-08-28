// Copyright © - Unpublished - Toby Hunter
using Newtonsoft.Json;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Services;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Models.Requests;
using ServerStatusSite.Models.Responses;
using ServerStatusSite.Models.Responses.Related;

namespace ServerStatusSite.Services
{
    public class BackupToolAPIService
    {
        private readonly ILoggerService _Logger;
        private readonly IBackupToolAPIClient _BackupToolAPIClient;
        private readonly RetryService _RetryService;

        // Sets the class's global variables.
        public BackupToolAPIService(
            ILoggerService _logger,
            IBackupToolAPIClient _backupToolAPIClient,
            RetryService _retryService)
        {
            _Logger = _logger;
            _BackupToolAPIClient = _backupToolAPIClient;
            _RetryService = _retryService;
        }

        /// <summary>
        /// Gets the live logs for a given server.
        /// </summary>
        public async Task<LogsResponseModel?> GetLogs(
            string serverName,
            string type = "All",
            string level = "All",
            int limit = 5000,
            int afterId = 0)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching logs from Backup Tool API for {serverName}");

            LogsResponseModel? logs = null;

            List<KeyValuePair<string, object>> queryParameters =
            [
                new("type", type),
                new("level", level),
                new("limit", limit)
            ];

            if (afterId > 0)
            {
                queryParameters.Add(new("afterId", afterId));
            }

            try
            {
                (logs, bool success) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.GetLogs(
                        serverName,
                        queryParameters),
                    result => result.Item2,
                    null,
                    $"fetch logs from Backup Tool API for {serverName}");

                if (success && logs != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {logs.ServerName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Logs Returned: {logs.Logs.Count}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Next After: {logs.NextAfter?.ToString() ?? "None"}");

                    foreach (LogEntryModel log in logs.Logs)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Log Id: {log.Id}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Timestamp: {log.Timestamp}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Level: {log.Level}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Type: {log.Type}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Message: {log.Message}");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Fetched logs from Backup Tool API for {serverName}");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to fetch logs from Backup Tool API for {serverName}");
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
                    $"Failed to fetch logs from Backup Tool API for {serverName}");
            }

            return logs;
        }

        /// <summary>
        /// Gets the list of archived log files for a given server.
        /// </summary>
        public async Task<LogArchivesResponseModel?> GetLogArchives(string serverName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching log archives from Backup Tool API for {serverName}");

            LogArchivesResponseModel? archives = null;

            try
            {
                (archives, bool success) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.GetLogArchives(serverName),
                    result => result.Item2,
                    null,
                    $"fetch log archives from Backup Tool API for {serverName}");

                if (success && archives != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {archives.ServerName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Archives Returned: {archives.Archives.Count}");

                    foreach (ArchivedLogFileModel archive in archives.Archives)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"File Name: {archive.FileName}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Created At: {archive.CreatedAt}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Size Bytes: {archive.SizeBytes}");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Fetched log archives from Backup Tool API for {serverName}");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to fetch log archives from Backup Tool API for {serverName}");
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
                    $"Failed to fetch log archives from Backup Tool API for {serverName}");
            }

            return archives;
        }

        /// <summary>
        /// Gets the logs from a specific archive file for a given server.
        /// </summary>
        public async Task<ArchivedLogsResponseModel?> GetArchivedLogs(
            string serverName,
            string fileName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching archived logs from Backup Tool API for {serverName} ({fileName})");

            ArchivedLogsResponseModel? archivedLogs = null;

            try
            {
                (archivedLogs, bool success) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.GetArchivedLogs(
                        serverName,
                        fileName),
                    result => result.Item2,
                    null,
                    $"fetch archived logs from Backup Tool API for {serverName} ({fileName})");

                if (success && archivedLogs != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {archivedLogs.ServerName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Archive Name: {archivedLogs.ArchiveName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Files Returned: {archivedLogs.Logs.Count}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Total Logs: {archivedLogs.Logs.Sum(l => l.Content.Count)}");

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Fetched archived logs from Backup Tool API for {serverName} ({fileName})");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to fetch archived logs from Backup Tool API for {serverName} ({fileName})");
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
                    $"Failed to fetch archived logs from Backup Tool API for {serverName} ({fileName})");
            }

            return archivedLogs;
        }

        /// <summary>
        /// Registers a webhook with the Backup Tool API for a given server.
        /// </summary>
        public async Task<WebhookRegistrationResponseModel?> RegisterWebhook(
            string serverName,
            string webhookUrl,
            string type = "All",
            string level = "All",
            int afterId = 0)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering webhook in Backup Tool API for {serverName}");

            WebhookRegistrationResponseModel? registration = null;

            string body = JsonConvert.SerializeObject(new
            {
                url = webhookUrl,
                serverName,
                logType = type,
                logLevel = level,
                afterId
            });

            try
            {
                (registration, bool success) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.RegisterWebhook(
                        serverName,
                        body),
                    result => result.Item2,
                    null,
                    $"register webhook in Backup Tool API for {serverName}");

                if (success && registration != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Registration Id: {registration.Id}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server Name: {registration.ServerName}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Registered webhook in Backup Tool API for {serverName}");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to register webhook in Backup Tool API for {serverName}");
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
                    $"Failed to register webhook in Backup Tool API for {serverName}");
            }

            return registration;
        }

        /// <summary>
        /// Unregisters a webhook from the Backup Tool API for a given server.
        /// </summary>
        public async Task<bool> UnregisterWebhook(
            string serverName,
            string webhookId)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Unregistering webhook, {webhookId}, from Backup Tool API for {serverName}");

            bool success = false;

            try
            {
                (success, _) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.UnregisterWebhook(
                        serverName,
                        webhookId),
                    result => result.Item1,
                    null,
                    $"unregister webhook, {webhookId}, from Backup Tool API for {serverName}");

                if (success)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Unregistered webhook, {webhookId}, from Backup Tool API for {serverName}");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to unregister webhook, {webhookId}, from Backup Tool API for {serverName}");
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
                    $"Failed to unregister webhook, {webhookId}, from Backup Tool API for {serverName}");
            }

            return success;
        }

        /// <summary>
        /// Sends a command to the Backup Tool API for a given server.
        /// </summary>
        public async Task<bool> SendCommand(
            string serverName,
            CommandRequestModel command)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Sending command to Backup Tool API for {serverName}");

            bool success = false;

            try
            {
                (success, _) = await _RetryService.ExecuteAsync(
                    () => _BackupToolAPIClient.SendCommand(
                        serverName,
                        command),
                    result => result.Item1,
                    null,
                    $"send command to Backup Tool API for {serverName}");

                if (success)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Sent command to Backup Tool API for {serverName}");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Failed to send command to Backup Tool API for {serverName}");
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
                    $"Failed to send command to Backup Tool API for {serverName}");
            }

            return success;
        }
    }
}
