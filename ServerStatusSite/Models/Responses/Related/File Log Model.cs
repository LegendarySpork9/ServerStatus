// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models.Responses.Related
{
    /// <summary>
    /// Stores the logs data from a single file within an archive.
    /// </summary>
    public class FileLogModel
    {
        public required string FileName { get; set; }
        public required List<LogEntryModel> Content { get; set; }
    }
}
