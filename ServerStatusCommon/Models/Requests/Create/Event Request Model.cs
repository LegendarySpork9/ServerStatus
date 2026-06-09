// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Requests.Create
{
    /// <summary>
    /// Stores the event data for the api request.
    /// </summary>
    public class EventRequestModel
    {
        public required string Component { get; set; }
        public required string Status { get; set; }
        public required int ServerId { get; set; }
        public required string Name { get; set; }
        public required string HostName { get; set; }
        public required string Game { get; set; }
        public required string GameVersion { get; set; }
    }
}
