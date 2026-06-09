// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;

namespace ServerStatusCommon.Functions
{
    public class TimerFunction
    {
        private readonly IClock _Clock;

        // Sets the class's global variables.
        public TimerFunction(
            IClock _clock)
        {
            _Clock = _clock;
        }

        /// <summary>
        /// Calculates the timer duration.
        /// </summary>
        public TimeSpan GetTimerInterval(DateTime nextElapse)
        {
            TimeSpan interval = nextElapse - _Clock.UtcNow;

            if (interval < TimeSpan.Zero)
            {
                interval = TimeSpan.Zero;
            }

            return interval;
        }
    }
}
