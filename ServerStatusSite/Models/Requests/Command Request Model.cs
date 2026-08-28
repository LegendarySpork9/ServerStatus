// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models.Requests
{
    /// <summary>
    /// Stores the command request data.
    /// </summary>
    public class CommandRequestModel
    {
        public required string Target { get; set; }
        public required string Command { get; set; }
    }
}
