// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Functions;

namespace ServerStatus.UnitTests.Common.Functions
{
    [TestClass]
    public class DateTimeFunctionTest
    {
        /// <summary>
        /// Checks that milliseconds below 500 round down to the current second.
        /// </summary>
        [TestMethod]
        public void TestRoundToNearestSecondRoundsDown()
        {
            DateTime value = new(2026, 09, 28, 10, 05, 58, 300, DateTimeKind.Utc);
            DateTime expected = new(2026, 09, 28, 10, 05, 58, 0, DateTimeKind.Utc);

            DateTime result = DateTimeFunction.RoundToNearestSecond(value);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Checks that milliseconds at or above 500 round up to the next second.
        /// </summary>
        [TestMethod]
        public void TestRoundToNearestSecondRoundsUp()
        {
            DateTime value = new(2026, 09, 28, 10, 05, 57, 999, DateTimeKind.Utc);
            DateTime expected = new(2026, 09, 28, 10, 05, 58, 0, DateTimeKind.Utc);

            DateTime result = DateTimeFunction.RoundToNearestSecond(value);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Checks that a value with no milliseconds is unchanged.
        /// </summary>
        [TestMethod]
        public void TestRoundToNearestSecondExactSecond()
        {
            DateTime value = new(2026, 09, 28, 10, 05, 58, 0, DateTimeKind.Utc);
            DateTime expected = new(2026, 09, 28, 10, 05, 58, 0, DateTimeKind.Utc);

            DateTime result = DateTimeFunction.RoundToNearestSecond(value);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Checks that exactly 500 milliseconds rounds up.
        /// </summary>
        [TestMethod]
        public void TestRoundToNearestSecondExactlyHalf()
        {
            DateTime value = new(2026, 09, 28, 10, 05, 57, 500, DateTimeKind.Utc);
            DateTime expected = new(2026, 09, 28, 10, 05, 58, 0, DateTimeKind.Utc);

            DateTime result = DateTimeFunction.RoundToNearestSecond(value);

            Assert.AreEqual(expected, result);
        }
    }
}
