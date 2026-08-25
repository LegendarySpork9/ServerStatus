// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusSite.Models.Responses
{
    /// <summary>
    /// Stores the Backup Tool registration response.
    /// </summary>
    public class WebhookRegistrationResponseModel
    {
        public required string Id { get; set; }
        public required string ServerName { get; set; }
    }
}
