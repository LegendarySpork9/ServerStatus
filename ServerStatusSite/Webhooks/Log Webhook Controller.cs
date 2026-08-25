// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusSite.Functions;
using ServerStatusSite.Models;
using ServerStatusSite.Models.Requests;
using ServerStatusSite.Services;

namespace ServerStatusSite.Webhooks
{
    /// <summary>
    /// Receives log webhook payloads from the Backup Tool API.
    /// </summary>
    [ApiController]
    [Route("webhooks")]
    public class LogWebhookController : ControllerBase
    {
        private readonly ILoggerService _Logger;
        private readonly LogStreamService _LogStream;
        private readonly BackupToolSettingsModel Settings;

        // Sets the class's global variables.
        public LogWebhookController(
            ILoggerService _logger,
            LogStreamService _logStream,
            BackupToolSettingsModel settings)
        {
            _Logger = _logger;
            _LogStream = _logStream;
            Settings = settings;
        }

        /// <summary>
        /// Receives logs from the Backup Tool and publishes them to subscribers.
        /// </summary>
        [HttpPost("serverlogs")]
        public async Task<IActionResult> ReceiveLogs()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Received Server Log Payload");

            try
            {
                using (StreamReader reader = new(Request.Body))
                {
                    string body = await reader.ReadToEndAsync();

                    string? signature = Request.Headers["X-Webhook-Secret"].FirstOrDefault();

                    if (!WebhookAuthValidationFunction.ValidateSignature(
                        signature,
                        body,
                        Settings.WebhookSecret))
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            "Invalid Webhook Signature");

                        return Unauthorized();
                    }

                    WebhookLogModel? payload = JsonConvert.DeserializeObject<WebhookLogModel>(body);

                    if (payload == null || string.IsNullOrEmpty(payload.ServerName) || payload.Logs == null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            "Invalid Webhook Payload");

                        return BadRequest();
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Server: {payload.ServerName}, Logs: {payload.Logs.Count}");

                    await _LogStream.Publish(
                        payload.ServerName,
                        payload.Logs);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Published Webhook Logs to Subscribers");

                    return Ok();
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

                return StatusCode(500);
            }
        }
    }
}
