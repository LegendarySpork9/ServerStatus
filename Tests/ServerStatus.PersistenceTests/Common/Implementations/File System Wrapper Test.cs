// Copyright © - 31/08/2026 - Toby Hunter
using ServerStatusCommon.Implementations;

namespace ServerStatus.PersistenceTests.Common.Implementations
{
    [TestClass]
    public class FileSystemWrapperTest
    {
        private string _TempDirectory = null!;

        /// <summary>
        /// Creates a temporary directory for test isolation.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _TempDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
            Directory.CreateDirectory(_TempDirectory);
        }

        /// <summary>
        /// Removes the temporary directory after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_TempDirectory))
            {
                Directory.Delete(_TempDirectory, true);
            }
        }

        /// <summary>
        /// Checks whether the ReadAllText method returns the contents of a file.
        /// </summary>
        [TestMethod]
        public async Task TestReadAllText()
        {
            FileSystemWrapper _wrapper = new();

            string filePath = Path.Combine(
                _TempDirectory,
                "test.txt");
            string expected = "Hello, World!";
            await File.WriteAllTextAsync(
                filePath,
                expected);

            string actual = await _wrapper.ReadAllText(filePath);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the ReadAllText method throws when the file does not exist.
        /// </summary>
        [TestMethod]
        public async Task TestReadAllTextFileNotFound()
        {
            FileSystemWrapper _wrapper = new();

            string filePath = Path.Combine(
                _TempDirectory,
                "nonexistent.txt");

            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
            {
                await _wrapper.ReadAllText(filePath);
            });
        }

        /// <summary>
        /// Checks whether the FileExists method returns true for an existing file.
        /// </summary>
        [TestMethod]
        public void TestFileExistsReturnsTrue()
        {
            FileSystemWrapper _wrapper = new();

            string filePath = Path.Combine(
                _TempDirectory,
                "exists.txt");
            File.WriteAllText(filePath, "content");

            bool actual = _wrapper.FileExists(filePath);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Checks whether the FileExists method returns false for a non-existing file.
        /// </summary>
        [TestMethod]
        public void TestFileExistsReturnsFalse()
        {
            FileSystemWrapper _wrapper = new();

            string filePath = Path.Combine(
                _TempDirectory,
                "nonexistent.txt");

            bool actual = _wrapper.FileExists(filePath);

            Assert.IsFalse(actual);
        }
    }
}
