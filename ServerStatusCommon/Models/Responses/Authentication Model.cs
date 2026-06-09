// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the authentication API response.
    /// </summary>
    public class AuthenticationModel
    {
        public required string Type { get; set; }
        public required string Token { get; set; }
        public required int ExpiresIn { get; set; }
        public required AuthenticationInfoModel Info { get; set; }
    }
}
