// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;

namespace ServerStatusCommon.Implementations
{
    public class HTTPClientWrapper : IHTTPClient
    {
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public HTTPClientWrapper(
            ILoggerService _logger)
        {
            _Logger = _logger;
        }

        /// <summary>
        /// Sends the given message.
        /// </summary>
        public async Task<HttpResponseMessage?> Send(HttpRequestMessage request)
        {
            HttpResponseMessage? response = null;

            try
            {
                HttpClient client = new();

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Configured Http Client");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "Sending Request");

                response = await client.SendAsync(request);
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

            return response;
        }
    }
}
