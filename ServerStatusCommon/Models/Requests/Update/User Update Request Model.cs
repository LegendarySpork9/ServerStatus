// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Requests.Update
{
    /// <summary>
    /// Stores the user data for the update api request.
    /// </summary>
    public class UserUpdateRequestModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
