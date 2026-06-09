// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Requests.Create
{
    /// <summary>
    /// Stores the alert data for the api request.
    /// </summary>
    public class AlertRequestModel
    {
        public required string Reporter { get; set; }
        public required string Component { get; set; }
        public required string ComponentStatus { get; set; }
        public required string AlertStatus { get; set; }
        public required int ServerId { get; set; }
        public required string Name { get; set; }
        public required string HostName { get; set; }
        public required string Game { get; set; }
        public required string GameVersion { get; set; }
    }
}
