// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models
{
    /// <summary>
    /// Stores the configuration for connecting to the Backup Tool API.
    /// </summary>
    public class BackupToolSettingsModel
    {
        public required string ApiUrlTemplate { get; set; }
        public required string WebhookSecret { get; set; }
        public required string SiteBaseUrl { get; set; }
        public Dictionary<string, ServerCredentialsModel> Servers { get; set; } = [];
    }
}
