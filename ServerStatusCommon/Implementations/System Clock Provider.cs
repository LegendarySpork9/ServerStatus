// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;

namespace ServerStatusCommon.Implementations
{
    public class SystemClockProvider : IClock
    {
        /// <summary>
        /// Returns the current UTC Date and time.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Returns the default date and time.
        /// </summary>
        public DateTime DefaultDate => new(1900, 01, 01, 0, 0, 0, DateTimeKind.Utc);
    }
}
