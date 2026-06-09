// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;

namespace ServerStatusCommon.Implementations
{
    public class LoggerServiceWrapper : ILoggerService
    {
        readonly LoggerService _Logger = new();

        /// <summary>
        /// Changes the identifier of the logger.
        /// </summary>
        public void ChangeIdentifier(string value) => _Logger.ChangeIdentifier(value);

        /// <summary>
        /// Logs the given message to the log file.
        /// </summary>
        public void LogMessage(string level, string message) => _Logger.LogMessage(level, message);
    }
}
