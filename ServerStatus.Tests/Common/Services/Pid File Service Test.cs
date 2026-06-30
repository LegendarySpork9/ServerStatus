// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusReporter.Services;

namespace ServerStatus.Tests.Common.Services
{
    [TestClass]
    public class PidFileServiceTest
    {
        /// <summary>
        /// Checks whether the Read method returns the correct data for a valid PID file.
        /// </summary>
        [TestMethod]
        public async Task TestRead()
        {
            DateTime startTime = new(2026, 06, 16, 14, 30, 0, DateTimeKind.Utc);
            string fileContent = $"12345\n{startTime:O}";

            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync(fileContent);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            (int processId, DateTime expectedStartTime)? result = await _pidFileService.Read("Test Server");

            Assert.IsNotNull(result);
            Assert.AreEqual(12345, result.Value.processId);
            Assert.AreEqual(startTime, result.Value.expectedStartTime);
        }

        /// <summary>
        /// Checks whether the Read method returns null when the PID file does not exist.
        /// </summary>
        [TestMethod]
        public async Task TestReadFileNotFound()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            var result = await _pidFileService.Read("Test Server");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the Read method returns null when the PID file is malformed.
        /// </summary>
        [TestMethod]
        public async Task TestReadMalformedFile()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("invalid content");

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            var result = await _pidFileService.Read("Test Server");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the Read method returns null when the PID file contains an invalid process ID.
        /// </summary>
        [TestMethod]
        public async Task TestReadInvalidProcessId()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("notanumber\n2026-06-16T14:30:00.0000000Z");

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            var result = await _pidFileService.Read("Test Server");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks whether the Read method returns null when the PID file contains an invalid start time.
        /// </summary>
        [TestMethod]
        public async Task TestReadInvalidStartTime()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("12345\nnotadate");

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            var result = await _pidFileService.Read("Test Server");

            Assert.IsNull(result);
        }
    }
}
