// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusReporter.Implementations;
using System.Diagnostics;

namespace ServerStatus.PersistenceTests.Reporter.Implementations
{
    [TestClass]
    public class ProcessServiceWrapperTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();

        /// <summary>
        /// Checks whether the IsRunning method returns true for the current process with the correct start time.
        /// </summary>
        [TestMethod]
        public void TestIsRunningReturnsTrueForCurrentProcess()
        {
            ProcessServiceWrapper wrapper = new(_MockLogger.Object);

            using Process current = Process.GetCurrentProcess();
            int processId = current.Id;
            DateTime startTime = current.StartTime.ToUniversalTime();

            bool result = wrapper.IsRunning(
                processId,
                startTime);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks whether the IsRunning method returns false for a non-existent process ID.
        /// </summary>
        [TestMethod]
        public void TestIsRunningReturnsFalseForNonExistentProcess()
        {
            ProcessServiceWrapper wrapper = new(_MockLogger.Object);

            bool result = wrapper.IsRunning(
                99999,
                DateTime.UtcNow);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks whether the IsRunning method returns false when the start time does not match.
        /// </summary>
        [TestMethod]
        public void TestIsRunningReturnsFalseForWrongStartTime()
        {
            ProcessServiceWrapper wrapper = new(_MockLogger.Object);

            using Process current = Process.GetCurrentProcess();
            int processId = current.Id;

            bool result = wrapper.IsRunning(
                processId,
                DateTime.UtcNow.AddDays(-1));

            Assert.IsFalse(result);
        }
    }
}
