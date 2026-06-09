// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the server API response.
    /// </summary>
    public class ServerModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string HostName { get; set; }
        public required string Game { get; set; }
        public required string GameVersion { get; set; }
        public required ConnectionModel Connection { get; set; }
        public required DowntimeModel? Downtime { get; set; }
        public required int EventInterval { get; set; }
        public required bool IsActive { get; set; }
    }
}
