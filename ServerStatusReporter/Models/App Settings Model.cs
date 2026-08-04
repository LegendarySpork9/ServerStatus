// Copyright © - 05/10/2025 - Toby Hunter
using System.Configuration;

namespace ServerStatusReporter.Models
{
    /// <summary>
    /// Stores the app specific settings.
    /// </summary>
    public static class AppSettingsModel
    {
        public static string[] Servers { get; set; } = ConfigurationManager.AppSettings["Servers"].Split(',');
        public static string[] Components { get; set; } = ConfigurationManager.AppSettings["Components"].Split(',');
    }
}
