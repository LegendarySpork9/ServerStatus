// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the status data.
    /// </summary>
    public class StatusModel
    {
        public required string Component { get; set; }
        public required string Status { get; set; }
        public required string StatusClass { get; set; }
    }
}
