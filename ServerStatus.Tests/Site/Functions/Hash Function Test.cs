// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusSite.Functions;

namespace ServerStatus.Tests.Site.Functions
{
    [TestClass]
    public class HashFunctionTest
    {
        /// <summary>
        /// Checks whether the HashString method returns the expected value.
        /// </summary>
        [TestMethod]
        public void TestHashString()
        {
            string hashedValue = HashFunction.HashString("8##XBbMT529*X0w!m$zvpmoq");

            Assert.AreEqual(
                "1b88b22568b87d4f401b47a29e904a4961c99e25b6f8da279be654af5c526b7c725bd3ec6295099bc9c0bf0a3c4aa089c4dbc63decd52dd9ac9779616f4a124f",
                hashedValue);
        }
    }
}
