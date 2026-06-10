// Copyright © - 05/10/2025 - Toby Hunter
using log4net;

namespace ServerStatusCommon.Services
{
    public class LoggerService
    {
        private readonly ILog Logger = LogManager.GetLogger("Logs");
        private string Identifier = "System";

        /// <summary>
        /// Updates the value of the Identifier variable.
        /// </summary>
        public void ChangeIdentifier(string value)
        {
            Identifier = value;
        }

        /// <summary>
        /// Sends a meessage to the specified logs.
        /// </summary>
        public void LogMessage(
            string level,
            string message)
        {
            switch (level)
            {
                case "Info": Logger.Info($"{Identifier} - {message.Trim()}"); break;
                case "Debug": Logger.Debug($"{Identifier} - {message.Trim()}"); break;
                case "Warn": Logger.Warn($"{Identifier} - {message.Trim()}"); break;
                case "Error": Logger.Error($"{Identifier} - {message.Trim()}"); break;
            }
        }
    }
}
