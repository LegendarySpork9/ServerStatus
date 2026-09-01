// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Implementations;

namespace ServerStatus.PersistenceTests.Common.Implementations
{
    [TestClass]
    public class SystemClockProviderTest
    {
        /// <summary>
        /// Checks whether the UtcNow property returns a UTC date and time.
        /// </summary>
        [TestMethod]
        public void TestUtcNowReturnsUtcKind()
        {
            SystemClockProvider _clock = new();

            DateTime expected = DateTime.UtcNow;
            DateTime actual = _clock.UtcNow;

            Assert.AreEqual(
                DateTimeKind.Utc,
                actual.Kind);
            Assert.IsTrue(actual >= expected.AddSeconds(-1));
        }

        /// <summary>
        /// Checks whether the UtcNow property returns a date close to the current time.
        /// </summary>
        [TestMethod]
        public void TestUtcNowReturnsCurrentTime()
        {
            SystemClockProvider _clock = new();

            DateTime before = DateTime.UtcNow;
            DateTime actual = _clock.UtcNow;
            DateTime after = DateTime.UtcNow;

            Assert.IsTrue(actual >= before);
            Assert.IsTrue(actual <= after);
        }

        /// <summary>
        /// Checks whether the DefaultDate property returns the expected date.
        /// </summary>
        [TestMethod]
        public void TestDefaultDateReturnsExpectedDate()
        {
            SystemClockProvider _clock = new();

            DateTime expected = new(1900, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime actual = _clock.DefaultDate;

            Assert.AreEqual(
                expected,
                actual);
        }
    }
}
