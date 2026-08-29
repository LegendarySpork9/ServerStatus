// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;

namespace ServerStatusSite.Models.Responses
{
    /// <summary>
    /// Stores the Backup Tool log response.
    /// </summary>
    public class LogsResponseModel
    {
        public required string ServerName { get; set; }
        public required List<LogEntryModel> Logs { get; set; }
        public int? NextAfter { get; set; }
    }
}
