// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the connection data.
    /// </summary>
    public class ConnectionModel
    {
        public required string IPAddress { get; set; }
        public required int Port { get; set; }
    }
}
