// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusSite.Converters;

namespace ServerStatus.UnitTests.Site.Converters
{
    [TestClass]
    public class StyleConverterTest
    {
        /// <summary>
        /// Checks whether the GetTopBarDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestTopBarDarkModeTrue()
        {
            string style = StyleConverter.GetTopBarDarkMode(true);

            Assert.AreEqual(
                "background-color: #3E3E3E; border: 1px solid transparent;",
                style);
        }

        /// <summary>
        /// Checks whether the GetTopBarDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestTopBarDarkModeFalse()
        {
            string style = StyleConverter.GetTopBarDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetTopNavLinkDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestTopNavLinkDarkModeTrue()
        {
            string style = StyleConverter.GetTopNavLinkDarkMode(true);

            Assert.AreEqual(
                "color: white;",
                style);
        }

        /// <summary>
        /// Checks whether the GetTopNavLinkDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestTopNavLinkDarkModeFalse()
        {
            string style = StyleConverter.GetTopNavLinkDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetBodyDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestBodyDarkModeTrue()
        {
            string style = StyleConverter.GetBodyDarkMode(true);

            Assert.AreEqual(
                "background-color: #313131; color: #A9A9A9;",
                style);
        }

        /// <summary>
        /// Checks whether the GetBodyDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestBodyDarkModeFalse()
        {
            string style = StyleConverter.GetBodyDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetNavMenuDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestNavMenuDarkModeTrue()
        {
            string style = StyleConverter.GetNavMenuDarkMode(true);

            Assert.AreEqual(
                "background-color: #4E4E4E; color: white;",
                style);
        }

        /// <summary>
        /// Checks whether the GetNavMenuDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestNavMenuDarkModeFalse()
        {
            string style = StyleConverter.GetNavMenuDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetTableDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestTableDarkModeTrue()
        {
            string style = StyleConverter.GetTableDarkMode(true);

            Assert.AreEqual(
                "color: #A9A9A9;",
                style);
        }

        /// <summary>
        /// Checks whether the GetTableDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestTableDarkModeFalse()
        {
            string style = StyleConverter.GetTableDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetFormDarkMode method returns the dark mode class when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestFormDarkModeTrue()
        {
            string style = StyleConverter.GetFormDarkMode(true);

            Assert.AreEqual(
                "form-dark",
                style);
        }

        /// <summary>
        /// Checks whether the GetFormDarkMode method returns the light mode class when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestFormDarkModeFalse()
        {
            string style = StyleConverter.GetFormDarkMode(false);

            Assert.AreEqual(
                "form-light",
                style);
        }

        /// <summary>
        /// Checks whether the GetInputDarkMode method returns the expected style when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestInputDarkModeTrue()
        {
            string style = StyleConverter.GetInputDarkMode(true);

            Assert.AreEqual(
                "background-color: #3E3E3E; color: #A9A9A9; border: 1px solid deepskyblue;",
                style);
        }

        /// <summary>
        /// Checks whether the GetInputDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestInputDarkModeFalse()
        {
            string style = StyleConverter.GetInputDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetTableRowDarkMode method returns the dark mode class when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestTableRowDarkModeTrue()
        {
            string style = StyleConverter.GetTableRowDarkMode(true);

            Assert.AreEqual(
                "dark-mode",
                style);
        }

        /// <summary>
        /// Checks whether the GetTableRowDarkMode method returns an empty string when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestTableRowDarkModeFalse()
        {
            string style = StyleConverter.GetTableRowDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        /// <summary>
        /// Checks whether the GetLoadingDarkMode method returns the light spinner class when dark mode is enabled.
        /// </summary>
        [TestMethod]
        public void TestLoadingDarkModeTrue()
        {
            string style = StyleConverter.GetLoadingDarkMode(true);

            Assert.AreEqual(
                "spinner-border text-light",
                style);
        }

        /// <summary>
        /// Checks whether the GetLoadingDarkMode method returns the default spinner class when dark mode is disabled.
        /// </summary>
        [TestMethod]
        public void TestLoadingDarkModeFalse()
        {
            string style = StyleConverter.GetLoadingDarkMode(false);

            Assert.AreEqual(
                "spinner-border",
                style);
        }
    }
}
