// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;

namespace ServerStatusCommon.Services
{
    public class PidFileService
    {
        private readonly ILoggerService _Logger;
        private readonly IFileSystem _FileSystem;

        private static readonly string PidDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Hunter Industries",
            "Server Backup Tool");

        // Sets the class's global variables.
        public PidFileService(
            ILoggerService logger,
            IFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Reads the PID file for the given server and returns the process ID and start time.
        /// </summary>
        public async Task<(int processId, DateTime startTimeUtc)?> Read(string serverName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Reading PID file for {serverName}");

            try
            {
                string filePath = Path.Combine(
                    PidDirectory,
                    $"{serverName}.pid");

                if (!_FileSystem.FileExists(filePath))
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"PID file not found for {serverName}");

                    return null;
                }

                string content = await _FileSystem.ReadAllText(filePath);
                string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"PID file for {serverName} is malformed");

                    return null;
                }

                if (!int.TryParse(lines[0].Trim(), out int processId))
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"PID file for {serverName} contains an invalid process ID");

                    return null;
                }

                if (!DateTime.TryParse(lines[1].Trim(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime startTimeUtc))
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"PID file for {serverName} contains an invalid start time");

                    return null;
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"PID file read for {serverName}: PID {processId}");

                return (processId, startTimeUtc);
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Failed to read PID file for {serverName}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());

                return null;
            }
        }
    }
}
