// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models.Responses.Related
{
    /// <summary>
    /// Stores the Backup Tool log data.
    /// </summary>
    public class LogEntryModel
    {
        public required int Id { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Level { get; set; }
        public required string Type { get; set; }
        public required string Message { get; set; }
    }
}
