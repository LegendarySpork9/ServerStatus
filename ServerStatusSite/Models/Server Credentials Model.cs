// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models
{
    /// <summary>
    /// Stores the credentials for a Backup Tool API instance.
    /// </summary>
    public class ServerCredentialsModel
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
    }
}
