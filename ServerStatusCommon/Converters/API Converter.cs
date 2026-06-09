// Copyright © - 05/10/2025 - Toby Hunter
namespace ServerStatusCommon.Converters
{
    public static class APIConverter
    {
        /// <summary>
        /// Returns the query parameters for a given endpoint.
        /// </summary>
        public static string GetQuery(string endpoint)
        {
            return endpoint switch
            {
                "/usersettings" => "?application=Server Status Site",
                "/serverstatus/serverinformation" => "?isActive=true",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS class of the status.
        /// </summary>
        public static string GetStatusClass(string status)
        {
            return status switch
            {
                "Online" => "online",
                "Offline" => "offline",
                "Unknown" => "unknown",
                _ => "unknown"
            };
        }
    }
}
