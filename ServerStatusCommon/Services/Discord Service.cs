// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models;
using System.Text;

namespace ServerStatusCommon.Services
{
    public class DiscordService
    {
        private readonly ILoggerService _Logger;
        private readonly IHTTPClient _HTTPClient;
        private readonly SharedSettingsModel SharedSettings;

        // Sets the class's global variables.
        public DiscordService(
            ILoggerService _logger,
            IHTTPClient _httpClient,
            SharedSettingsModel sharedSettings)
        {
            _Logger = _logger;
            _HTTPClient = _httpClient;
            SharedSettings = sharedSettings;
        }

        /// <summary>
        /// Sends a message to the given webhook URL.
        /// </summary>
        public async Task<bool> SendNotification(
            string recipientId,
            string message)
        {
            if (SharedSettings.SendAlerts)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Sending Notification to Discord");

                bool successfulSend = false;

                try
                {
                    string url = SharedSettings.WebhookURL + "?wait=true";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"URL: {url}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Recipient: {recipientId}");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Message: {message}");

                    string payload = "{\"content\": \"<@&" + recipientId + "> " + message + "\"}";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Payload: {payload}");

                    HttpContent content = new StringContent(
                        payload,
                        Encoding.UTF8,
                        "application/json");

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Http Content");

                    HttpRequestMessage request = new(
                        HttpMethod.Post,
                        url)
                    {
                        Content = content
                    };

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Http Request Message");

                    HttpResponseMessage? response = await _HTTPClient.Send(request);

                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        successfulSend = true;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Response Code: {response.StatusCode}");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Response Message: {response.Content}");
                    }
                }

                catch (Exception ex)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        ex.Message);
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        ex.ToString());
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Sent Notification to Discord");
                return successfulSend;
            }

            return true;
        }
    }
}
