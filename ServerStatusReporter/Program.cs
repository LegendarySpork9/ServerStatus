// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Services;
using ServerStatusReporter.Abstractions;
using ServerStatusReporter.Implementations;
using ServerStatusReporter.Models;
using ServerStatusReporter.Services;
using System.Reflection;

namespace ServerStatusReporter
{
    internal class Program
    {
        // Configures the application at startup.
        static async Task Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            ILoggerService _logger = new LoggerServiceWrapper();
            _logger.ChangeIdentifier("Reporter");

            SharedSettingsModel sharedSettings = SharedSettingsLoader.LoadSettingsFromConfig(SharedSettingsLoader.LoadConfig($"{Assembly.GetExecutingAssembly().Location}.config"));

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Logging Started");
            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuring Application");
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
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Host Name: {AppSettingsModel.HostName}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Games: {string.Join(',', AppSettingsModel.Games)}");
            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Components: {string.Join(',', AppSettingsModel.Components)}");

            IClock _clock = new SystemClockProvider();
            IFileSystem _fileSystem = new FileSystemWrapper();
            IProcessService _processService = new ProcessServiceWrapper(_logger);
            APIClientWrapper _apiClient = new(
                _logger,
                _fileSystem,
                sharedSettings);
            RetryService _retryService = new RetryService(_logger);
            APIService _apiService = new(
                _logger,
                _apiClient,
                _clock,
                _retryService);
            PidFileService _pidFileService = new(
                _logger,
                _fileSystem);
            ApplicationService _applicationService = new(
                _logger,
                _clock,
                new TCPClientWrapper(_logger),
                _processService,
                _apiService,
                _pidFileService,
                sharedSettings);
            _applicationService.Setup();

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configured Application");

            await _applicationService.Start();

            Console.ReadLine();

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Logging Stopped");
        }
    }
}
