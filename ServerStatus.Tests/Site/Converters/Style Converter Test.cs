// Copyright © - 05/10/2025 - Toby Hunter
using ServerStatusSite.Converters;

namespace ServerSite.Tests.Site.Converters
{
    [TestClass]
    public class StyleConverterTest
    {
        [TestMethod]
        public void TestTopBarDarkModeTrue()
        {
            string style = StyleConverter.GetTopBarDarkMode(true);

            Assert.AreEqual(
                "background-color: #3E3E3E; border: 1px solid transparent;",
                style);
        }

        [TestMethod]
        public void TestTopBarDarkModeFalse()
        {
            string style = StyleConverter.GetTopBarDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestTopNavLinkDarkModeTrue()
        {
            string style = StyleConverter.GetTopNavLinkDarkMode(true);

            Assert.AreEqual(
                "color: white;",
                style);
        }

        [TestMethod]
        public void TestTopNavLinkDarkModeFalse()
        {
            string style = StyleConverter.GetTopNavLinkDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestBodyDarkModeTrue()
        {
            string style = StyleConverter.GetBodyDarkMode(true);

            Assert.AreEqual(
                "background-color: #313131; color: #A9A9A9;",
                style);
        }

        [TestMethod]
        public void TestBodyDarkModeFalse()
        {
            string style = StyleConverter.GetBodyDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestNavMenuDarkModeTrue()
        {
            string style = StyleConverter.GetNavMenuDarkMode(true);

            Assert.AreEqual(
                "background-color: #4E4E4E; color: white;",
                style);
        }

        [TestMethod]
        public void TestNavMenuDarkModeFalse()
        {
            string style = StyleConverter.GetNavMenuDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestTableDarkModeTrue()
        {
            string style = StyleConverter.GetTableDarkMode(true);

            Assert.AreEqual(
                "color: #A9A9A9;",
                style);
        }

        [TestMethod]
        public void TestTableDarkModeFalse()
        {
            string style = StyleConverter.GetTableDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestFormDarkModeTrue()
        {
            string style = StyleConverter.GetFormDarkMode(true);

            Assert.AreEqual(
                "form-dark",
                style);
        }

        [TestMethod]
        public void TestFormDarkModeFalse()
        {
            string style = StyleConverter.GetFormDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestInputDarkModeTrue()
        {
            string style = StyleConverter.GetInputDarkMode(true);

            Assert.AreEqual(
                "background-color: #3E3E3E; color: #A9A9A9; border: 1px solid deepskyblue;",
                style);
        }

        [TestMethod]
        public void TestInputDarkModeFalse()
        {
            string style = StyleConverter.GetInputDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestTableRowDarkModeTrue()
        {
            string style = StyleConverter.GetTableRowDarkMode(true);

            Assert.AreEqual(
                "dark-mode",
                style);
        }

        [TestMethod]
        public void TestTableRowDarkModeFalse()
        {
            string style = StyleConverter.GetTableRowDarkMode(false);

            Assert.AreEqual(
                string.Empty,
                style);
        }

        [TestMethod]
        public void TestLoadingDarkModeTrue()
        {
            string style = StyleConverter.GetLoadingDarkMode(true);

            Assert.AreEqual(
                "spinner-border text-light",
                style);
        }

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
