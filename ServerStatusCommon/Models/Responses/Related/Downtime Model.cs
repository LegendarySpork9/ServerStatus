// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the downtime data.
    /// </summary>
    public class DowntimeModel
    {
        public required string Time { get; set; }
        public required int Duration { get; set; }
    }
}
