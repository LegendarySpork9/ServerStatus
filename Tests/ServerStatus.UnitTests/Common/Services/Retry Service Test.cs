// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Services;

namespace ServerStatus.UnitTests.Common.Services
{
    [TestClass]
    public class RetryServiceTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();

        /// <summary>
        /// Checks whether the ExecuteAsync method returns the result on the first attempt when successful.
        /// </summary>
        [TestMethod]
        public async Task TestExecuteAsyncSuccessFirstAttempt()
        {
            RetryService _retryService = new(_MockLogger.Object);

            int expected = 42;
            int actual = await _retryService.ExecuteAsync(
                () => Task.FromResult(42),
                result => result == 42,
                null,
                "test operation",
                maxRetries: 3,
                delaySeconds: 0);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the ExecuteAsync method retries and returns the result on a subsequent attempt.
        /// </summary>
        [TestMethod]
        public async Task TestExecuteAsyncSuccessAfterRetry()
        {
            RetryService _retryService = new(_MockLogger.Object);

            int callCount = 0;

            int expected = 99;
            int actual = await _retryService.ExecuteAsync(
                () =>
                {
                    callCount++;
                    return Task.FromResult(callCount >= 3 ? 99 : 0);
                },
                result => result == 99,
                null,
                "test operation",
                maxRetries: 4,
                delaySeconds: 0);

            Assert.AreEqual(
                expected,
                actual);
            Assert.AreEqual(
                3,
                callCount);
        }

        /// <summary>
        /// Checks whether the ExecuteAsync method returns the default result when all retries fail.
        /// </summary>
        [TestMethod]
        public async Task TestExecuteAsyncAllRetriesFail()
        {
            RetryService _retryService = new(_MockLogger.Object);

            int expected = 0;
            int actual = await _retryService.ExecuteAsync(
                () => Task.FromResult(0),
                result => result == 99,
                null,
                "test operation",
                maxRetries: 2,
                delaySeconds: 0);

            Assert.AreEqual(
                expected,
                actual);

            _MockLogger.Verify(
                l => l.LogMessage("Info", It.Is<string>(s => s.Contains("Failed to test operation"))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the ExecuteAsync method handles exceptions and retries.
        /// </summary>
        [TestMethod]
        public async Task TestExecuteAsyncExceptionHandling()
        {
            RetryService _retryService = new(_MockLogger.Object);

            int callCount = 0;

            int expected = 1;
            int actual = await _retryService.ExecuteAsync(
                () =>
                {
                    callCount++;

                    if (callCount == 1)
                    {
                        throw new InvalidOperationException("Test exception");
                    }

                    return Task.FromResult(1);
                },
                result => result == 1,
                null,
                "test operation",
                maxRetries: 2,
                delaySeconds: 0);

            Assert.AreEqual(
                expected,
                actual);
            Assert.AreEqual(
                2,
                callCount);

            _MockLogger.Verify(
                l => l.LogMessage("Warn", It.Is<string>(s => s.Contains("Test exception"))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the ExecuteAsync method calls the onBeforeRetry callback before each retry.
        /// </summary>
        [TestMethod]
        public async Task TestExecuteAsyncCallsOnBeforeRetry()
        {
            RetryService _retryService = new(_MockLogger.Object);

            int beforeRetryCount = 0;
            int callCount = 0;

            await _retryService.ExecuteAsync(
                () =>
                {
                    callCount++;
                    return Task.FromResult(callCount >= 3 ? 1 : 0);
                },
                result => result == 1,
                () =>
                {
                    beforeRetryCount++;
                    return Task.CompletedTask;
                },
                "test operation",
                maxRetries: 4,
                delaySeconds: 0);

            Assert.AreEqual(
                2,
                beforeRetryCount);
        }
    }
}
