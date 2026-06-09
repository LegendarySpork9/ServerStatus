// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the authentication information.
    /// </summary>
    public class AuthenticationInfoModel
    {
        public required string ApplicationName { get; set; }
        public required List<string> Scopes { get; set; }
        public required DateTime Issued { get; set; }
        public required DateTime Expires { get; set; }
    }
}
