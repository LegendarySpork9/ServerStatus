// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses;

namespace ServerStatusSite.Abstractions
{
    /// <summary>
    /// Interface for the Backup Tool API.
    /// </summary>
    public interface IBackupToolAPIClient
    {
        Task<(LogsResponseModel?, bool)> GetLogs(string serverName, List<KeyValuePair<string, object>> queryParameters);
        Task<(LogArchivesResponseModel?, bool)> GetLogArchives(string serverName);
        Task<(ArchivedLogsResponseModel?, bool)> GetArchivedLogs(string serverName, string fileName);
        Task<(WebhookRegistrationResponseModel?, bool)> RegisterWebhook(string serverName, string body);
        Task<(bool, bool)> UnregisterWebhook(string serverName, string webhookId);
    }
}
