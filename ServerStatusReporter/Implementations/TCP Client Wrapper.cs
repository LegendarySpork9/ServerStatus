// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusReporter.Abstractions;
using System.Net.Sockets;

namespace ServerStatusReporter.Implementations
{
    public class TCPClientWrapper : ITCPClient
    {
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public TCPClientWrapper(
            ILoggerService _logger)
        {
            _Logger = _logger;
        }

        /// <summary>
        /// Checks whether an IP and port can be pinged.
        /// </summary>
        public async Task<bool> PingAddress(string ipAddress, int port)
        {
            bool success = false;

            try
            {
                using (TcpClient client = new())
                {
                    _Logger.LogMessage(StandardValues.LoggerValues.Debug, "Configured TCP Client");
                    _Logger.LogMessage(StandardValues.LoggerValues.Debug, "Pinging Address");

                    using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(5)))
                    {
                        await client.ConnectAsync(ipAddress, port, cts.Token);
                        success = true;
                    }
                }
            }

            catch (OperationCanceledException)
            {
                _Logger.LogMessage(StandardValues.LoggerValues.Debug, "Connection timed out");
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(StandardValues.LoggerValues.Warning, ex.Message);
                _Logger.LogMessage(StandardValues.LoggerValues.Error, ex.ToString());
            }

            return success;
        }
    }
}
