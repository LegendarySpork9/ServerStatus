// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the alert data.
    /// </summary>
    public class AlertModel
    {
        public required int Id { get; set; }
        public required string Reporter { get; set; }
        public required string Component { get; set; }
        public required string ComponentStatus { get; set; }
        public required string AlertStatus { get; set; }
        public required DateTime AlertDate { get; set; }
        public required RelatedServerModel Server { get; set; }
    }
}
