// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Functions
{
    public static class DateTimeFunction
    {
        /// <summary>
        /// Rounds a DateTime to the nearest whole second.
        /// </summary>
        public static DateTime RoundToNearestSecond(DateTime value)
        {
            DateTime rounded = value.AddMilliseconds(500);
            return rounded.AddTicks(-(rounded.Ticks % TimeSpan.TicksPerSecond));
        }
    }
}
