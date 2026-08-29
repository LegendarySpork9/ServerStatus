// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models.Responses.Related
{
    /// <summary>
    /// Stores the archived log data.
    /// </summary>
    public class ArchivedLogFileModel
    {
        public required string FileName { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required long SizeBytes { get; set; }
    }
}
