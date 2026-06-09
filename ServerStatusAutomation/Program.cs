// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusAutomation.Services;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Services;
using System.Reflection;

namespace ServerStatusAutomation
{
    internal class Program
    {
        /// <summary>
        /// Configures the application at startup.
        /// </summary>
        static async Task Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            ILoggerService _logger = new LoggerServiceWrapper();
            _logger.ChangeIdentifier("Automation");

            SharedSettingsModel sharedSettings = SharedSettingsLoader.LoadSettingsFromConfig(SharedSettingsLoader.LoadConfig($"{Assembly.GetExecutingAssembly().Location}.config"));

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Logging Started");
            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Application");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Webhook URL: {sharedSettings.WebhookURL}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Recipient Id: {sharedSettings.RecipientId}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"API Base URL: {sharedSettings.BaseURL}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"API Credentials: {sharedSettings.Credentials}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"API Auth Payload Location: {sharedSettings.AuthPayloadLocation}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Refresh Time: {sharedSettings.RefreshTime}");

            IClock _clock = new SystemClockProvider();
            APIClientWrapper _apiClient = new(
                _logger,
                new FileSystemWrapper(),
                sharedSettings);
            APIService _apiService = new(
                _logger,
                _apiClient,
                _clock);
            AutomationService _automationService = new(
                _logger,
                _clock,
                new HTTPClientWrapper(_logger),
                _apiService,
                sharedSettings);
            _automationService.Setup();

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configured Application");

            await _automationService.Start();

            Console.ReadLine();

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Logging Stopped");
        }
    }
}
