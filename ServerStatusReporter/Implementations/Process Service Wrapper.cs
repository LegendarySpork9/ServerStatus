// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusReporter.Abstractions;
using System.Diagnostics;

namespace ServerStatusReporter.Implementations
{
    public class ProcessServiceWrapper : IProcessService
    {
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public ProcessServiceWrapper(
            ILoggerService _logger)
        {
            _Logger = _logger;
        }

        /// <summary>
        /// Checks whether a process with the given ID is running and matches the expected start time.
        /// </summary>
        public bool IsRunning(
            int processId,
            DateTime expectedStartTime)
        {
            bool running = false;

            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    running = !process.HasExited && process.StartTime.ToUniversalTime() == expectedStartTime;
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Process {processId} running: {running}");
            }

            catch (ArgumentException)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Process {processId} not found");
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

            return running;
        }
    }
}
