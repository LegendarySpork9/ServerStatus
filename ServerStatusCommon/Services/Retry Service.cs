// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;

namespace ServerStatusCommon.Services
{
    public class RetryService : IRetryService
    {
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public RetryService(ILoggerService _logger)
        {
            _Logger = _logger;
        }

        /// <summary>
        /// Executes the given action with retry logic.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            Func<T, bool> isSuccess,
            Func<Task>? onBeforeRetry,
            string operationName,
            int maxRetries = 4,
            int delaySeconds = 30)
        {
            T result = default!;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Retry {attempt} of {maxRetries}");

                    if (onBeforeRetry != null)
                    {
                        await onBeforeRetry();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }

                try
                {
                    result = await action();

                    if (isSuccess(result))
                    {
                        return result;
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
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Failed to {operationName}");

            return result;
        }
    }
}
