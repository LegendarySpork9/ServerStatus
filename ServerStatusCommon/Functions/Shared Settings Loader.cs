// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models;
using ServerStatusCommon.Services;
using System.Configuration;
using System.Reflection;

namespace ServerStatusCommon.Functions
{
    public static class SharedSettingsLoader
    {
        /// <summary>
        /// Loads the given configuration file.
        /// </summary>
        public static Configuration LoadConfig(string config) => ConfigurationManager.OpenMappedExeConfiguration(new()
        {
            ExeConfigFilename = config
        },
        ConfigurationUserLevel.None);

        /// <summary>
        /// Loads the app settings dynamically from the App.config.
        /// </summary>
        public static SharedSettingsModel LoadSettingsFromConfig(Configuration config)
        {
            LoggerService _logger = new();

            SharedSettingsModel sharedSettings = new();
            PropertyInfo[] properties = typeof(SharedSettingsModel).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object? configurationValue = config.AppSettings.Settings[property.Name]?.Value;

                if (configurationValue != null)
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(
                            configurationValue,
                            property.PropertyType);
                        property.SetValue(
                            sharedSettings,
                            convertedValue);
                    }

                    catch (Exception ex)
                    {
                        _logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            ex.Message);
                        _logger.LogMessage(
                            StandardValues.LoggerValues.Error,
                            ex.ToString());
                    }
                }
            }

            return sharedSettings;
        }
    }
}
