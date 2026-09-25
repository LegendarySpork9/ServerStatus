// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Values
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
        /// Standard Component Values.
        /// </summary>
        public static class ComponentValues
        {
            public const string PC = "PC";
            public const string Server = "Server";
            public const string Connection = "Connection";
        }

        /// <summary>
        /// Standard Status Values.
        /// </summary>
        public static class StatusValues
        {
            public const string Online = "Online";
            public const string Offline = "Offline";
            public const string Unknown = "Unknown";
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
                ComponentStatus = StatusValues.Offline,
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
            public static readonly PagedResponseModel<AlertModel> DefaultAlertInfo = new()
            {
                Entries = [DefaultAlert],
                EntryCount = 1,
                PageNumber = 1,
                PageSize = 25,
                TotalPageCount = 1,
                TotalCount = 1
            };
        }
    }
}
