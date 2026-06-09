// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Converters;

namespace ServerStatus.Tests.Common.Converters
{
    [TestClass]
    public class APIConverterTest
    {
        #region GetQuery

        /// <summary>
        /// Checks whether the GetQuery method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetQuery()
        {
            string expected = string.Empty;
            string actual = APIConverter.GetQuery("/endpoint");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetQuery method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetQueryUserSettings()
        {
            string expected = "?application=Server Status Site";
            string actual = APIConverter.GetQuery("/usersettings");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetQuery method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetQueryServerInformation()
        {
            string expected = "?isActive=true";
            string actual = APIConverter.GetQuery("/serverstatus/serverinformation");

            Assert.AreEqual(
                expected,
                actual);
        }

        #endregion

        #region GetStatusClass

        /// <summary>
        /// Checks whether the GetStatusClass method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetStatusClassOnline()
        {
            string expected = "online";
            string actual = APIConverter.GetStatusClass("Online");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetStatusClass method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetStatusClassOffline()
        {
            string expected = "offline";
            string actual = APIConverter.GetStatusClass("Offline");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetStatusClass method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetStatusClassUnknown()
        {
            string expected = "unknown";
            string actual = APIConverter.GetStatusClass("Unknown");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetStatusClass method returns the correct output for the given value.
        /// </summary>
        [TestMethod]
        public void TestGetStatusClassOther()
        {
            string expected = "unknown";
            string actual = APIConverter.GetStatusClass("Active");

            Assert.AreEqual(
                expected,
                actual);
        }

        #endregion
    }
}
