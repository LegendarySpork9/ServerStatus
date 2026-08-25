// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;

namespace ServerStatusSite.Models.Responses
{
    /// <summary>
    /// Stores the Backup Tool log archive response.
    /// </summary>
    public class LogArchivesResponseModel
    {
        public required string ServerName { get; set; }
        public required List<ArchivedLogFileModel> Archives { get; set; }
    }
}
