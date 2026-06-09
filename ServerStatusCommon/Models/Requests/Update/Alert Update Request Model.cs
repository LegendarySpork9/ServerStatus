// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Requests.Update
{
    /// <summary>
    /// Stores the alert data for the update api request.
    /// </summary>
    public class AlertUpdateRequestModel
    {
        public required string Status { get; set; }
    }
}
