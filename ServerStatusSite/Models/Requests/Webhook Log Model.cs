// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;

namespace ServerStatusSite.Models.Requests
{
    /// <summary>
    /// Stores the webhook log data.
    /// </summary>
    public class WebhookLogModel
    {
        public required string ServerName { get; set; }
        public required List<LogEntryModel> Logs { get; set; }
    }
}
