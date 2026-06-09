// Copyright © - 05/10/2025 - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Functions;

namespace ServerStatus.Tests.Common.Functions
{
    [TestClass]
    public class TimerFunctionTest
    {
        /// <summary>
        /// Checks the GetTimerInterval method output with a given time.
        /// </summary>
        [TestMethod]
        public void TestGetTimerInterval()
        {
            DateTime utcNow = new(2026, 03, 12, 15, 20, 00, DateTimeKind.Utc);

            Mock<IClock> _mockClock = new();
            _mockClock.Setup(c => c.UtcNow)
                .Returns(utcNow);

            TimerFunction _timerFunction = new(_mockClock.Object);

            DateTime now = utcNow;
            TimeSpan interval = _timerFunction.GetTimerInterval(now.AddMilliseconds(-now.Millisecond)
                .AddMinutes(5));

            Assert.IsTrue(interval > TimeSpan.Zero);
        }

        /// <summary>
        /// Checks the GetTimerInterval method output with a given time.
        /// </summary>
        [TestMethod]
        public void TestGetTimerIntervalFail()
        {
            DateTime utcNow = new(2026, 03, 12, 15, 20, 00, DateTimeKind.Utc);

            Mock<IClock> _mockClock = new();
            _mockClock.Setup(c => c.UtcNow)
                .Returns(utcNow);

            TimerFunction _timerFunction = new(_mockClock.Object);

            TimeSpan interval = _timerFunction.GetTimerInterval(utcNow);

            Assert.IsTrue(interval == TimeSpan.Zero);
        }
    }
}
