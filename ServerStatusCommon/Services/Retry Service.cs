// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Values;

namespace ServerStatusCommon.Services
{
    public class RetryService
    {
        private readonly ILoggerService _Logger;
        private readonly int _DelaySeconds = 10;
        private readonly int _MaxRetries = 4;

        // Sets the class's global variables.
        public RetryService(ILoggerService _logger)
        {
            _Logger = _logger;
        }

        // Set's the class's global variables.
        internal RetryService(
            ILoggerService _logger,
            int delaySeconds,
            int maxRetries)
        {
            _Logger = _logger;
            _DelaySeconds = delaySeconds;
            _MaxRetries = maxRetries;
        }

        /// <summary>
        /// Executes the given action with retry logic.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            Func<T, bool> isSuccess,
            Func<Task>? onBeforeRetry)
        {
            T result = default!;

            for (int attempt = 0; attempt <= _MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Retry {attempt} of {_MaxRetries}");

                    if (onBeforeRetry != null)
                    {
                        await onBeforeRetry();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_DelaySeconds));
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

            return result;
        }
    }
}
