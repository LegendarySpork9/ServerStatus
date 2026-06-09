// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the server data.
    /// </summary>
    public class RelatedServerModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string HostName { get; set; }
        public required string Game { get; set; }
        public required string GameVersion { get; set; }
    }
}
