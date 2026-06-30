// Copyright © - 05/10/2025 - Toby Hunter
using System.Configuration;

namespace ServerStatusReporter.Models
{
    /// <summary>
    /// Stores the app specific settings.
    /// </summary>
    public static class AppSettingsModel
    {
        public static string HostName { get; set; } = ConfigurationManager.AppSettings["HostName"];
        public static string[] Games { get; set; } = ConfigurationManager.AppSettings["Games"].Split(',');
        public static string[] Components { get; set; } = ConfigurationManager.AppSettings["Components"].Split(',');
    }
}
