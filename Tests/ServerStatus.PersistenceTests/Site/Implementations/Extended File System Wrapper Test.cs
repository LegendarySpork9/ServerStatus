// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Implementations;

namespace ServerStatus.PersistenceTests.Site.Implementations
{
    [TestClass]
    public class ExtendedFileSystemWrapperTest
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
        /// Checks whether the WriteAllText method writes the contents to a file.
        /// </summary>
        [TestMethod]
        public async Task TestWriteAllText()
        {
            ExtendedFileSystemWrapper _wrapper = new();

            string filePath = Path.Combine(
                _TempDirectory,
                "write-test.txt");
            string expected = "Hello, World!";

            await _wrapper.WriteAllText(
                filePath,
                expected);

            string actual = await File.ReadAllTextAsync(filePath);

            Assert.AreEqual(
                expected,
                actual);
        }
    }
}
