// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Converters
{
    public static class StandardValues
    {
        /// <summary>
        /// Standard Logger Values.
        /// </summary>
        public static class LoggerValues
        {
            public const string Debug = "Debug";
            public const string Error = "Error";
            public const string Info = "Info";
            public const string Warning = "Warn";
        }

        /// <summary>
        /// Standard Setting Values.
        /// </summary>
        public static class SettingValues
        {
            public static readonly List<SettingModel> Default =
            [
                new()
                {
                    Id = 0,
                    Name = "DarkMode",
                    Value = "false"
                },
                new()
                {
                    Id = 0,
                    Name = "DiscordName",
                    Value = string.Empty
                },
                new()
                {
                    Id = 0,
                    Name = "IsAdmin",
                    Value = "false"
                }
            ];
        }

        /// <summary>
        /// Standard Alert Values.
        /// </summary>
        public static class AlertValues
        {
            public static readonly AlertModel DefaultAlert = new()
            {
                Id = 0,
                Reporter = "",
                Component = "",
                ComponentStatus = "Offline",
                AlertStatus = "Reported",
                AlertDate = DateTime.UtcNow,
                Server = new()
                {
                    Id = 0,
                    Name = "",
                    HostName = "",
                    Game = "",
                    GameVersion = ""
                }
            };
            public static readonly AlertInformationModel DefaultAlertInfo = new()
            {
                Entries = [DefaultAlert],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };
        }

        /// <summary>
        /// Standard Missing Values.
        /// </summary>
        public static class MissingValues
        {
            public const string DT = "01/01/1900 00:00:00";
            public const string Integer = "0";

            public const string HostName = "HostNotFound";
            public const string Game = "GameNotFound";
            public const string GameVersion = "GameVersionNotFound";
            public const string IpAddress = "127.0.0.1";
            public const string Port = "443";

            public const string ResponseContent = "\"{\\\"information\\\":\\\"ResponseContentNotFound\\\"}\"";
            public const string RelatedContent = "\"{\\\"information\\\":\\\"RelatedContentNotFound\\\"}\"";

            public const string AuthEndpoint = "/auth/token";
            public const string BearerToken = "BearerTokenNotObtained";

            public const string UserEndpoint = "/user";
            public const string Username = "UsernameNotObtained";
            public const string Password = "PasswordNotObtained";

            public const string SettingsEndpoint = "/usersettings";
            public const string SettingStringValue = "ValueNotFound";
            public const string SettingBoolValue = "false";

            public const string ServerEndpoint = "/serverstatus/serverinformation";

            public const string StatusEndpoint = "/serverstatus/serverevent?";
            public const string Component = "PC";
            public const string Status = "Offline";

            public const string AlertEndpoint = "/serverstatus/serveralert";
            public const string Reporter = "ReporterNotFound";
            public const string AlertStatus = "Reported";
        }
    }
}
