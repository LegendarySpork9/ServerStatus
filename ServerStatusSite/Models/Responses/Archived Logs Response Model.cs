// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;

namespace ServerStatusSite.Models.Responses
{
    /// <summary>
    /// Stores the Backup Tool archived logs response.
    /// </summary>
    public class ArchivedLogsResponseModel
    {
        public required string ServerName { get; set; }
        public required string ArchiveName { get; set; }
        public required List<FileLogModel> Logs { get; set; }
    }
}
