// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;

namespace ServerStatusReporter.Services
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
        public async Task<(int, DateTime)?> Read(string serverName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Reading PID file for {serverName}");

            int processId = 0;
            DateTime startTime = default;
            bool proceed = true;

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

                    proceed = false;
                }

                if (proceed)
                {
                    string content = await _FileSystem.ReadAllText(filePath);
                    string[] lines = content.Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length < 2)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"PID file for {serverName} is malformed");

                        proceed = false;
                    }

                    if (proceed && !int.TryParse(
                        lines[0].Trim(),
                        out processId))
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"PID file for {serverName} contains an invalid process ID");

                        proceed = false;
                    }

                    if (proceed && !DateTime.TryParse(
                        lines[1].Trim(),
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out startTime))
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"PID file for {serverName} contains an invalid start time");

                        proceed = false;
                    }
                }

                if (proceed)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"PID file read for {serverName}: PID {processId}");
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Failed to read PID file for {serverName}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());

                proceed = false;
            }

            return proceed
                ? (processId, startTime)
                : null;
        }
    }
}
