// Copyright © - 05/10/2025 - Toby Hunter
using FluentAssertions;
using ServerStatusCommon.Functions;
using ServerStatusCommon.Models;
using System.Configuration;

namespace ServerStatus.Tests.Common.Functions
{
    [TestClass]
    public class SharedSettingsLoaderTest
    {
        /// <summary>
        /// Checks the LoadConfig method returns the correct configuration.
        /// </summary>
        [TestMethod]
        public void TestLoadConfig()
        {
            Configuration result = SharedSettingsLoader.LoadConfig(Path.Combine(
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")),
                @"Mocks\Configs\Test.config"));

            Assert.IsTrue(result.AppSettings.Settings.Count == 2);
            Assert.AreEqual(
                "This is a test",
                result.AppSettings.Settings["TestSetting"].Value);
            Assert.AreEqual(
                "Second test incoming",
                result.AppSettings.Settings["TestSettingTwo"].Value);
        }

        /// <summary>
        /// Checks the LoadConfig method fails to return a configuration.
        /// </summary>
        [TestMethod]
        public void TestLoadConfigFail()
        {
            Configuration result = SharedSettingsLoader.LoadConfig(Path.Combine(
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")),
                @"Mocks\Configs\Test Two.config"));

            Assert.IsTrue(result.AppSettings.Settings.Count == 0);
        }

        /// <summary>
        /// Checks the LoadSettingsFromConfig method outputs all settings in the config.
        /// </summary>
        [TestMethod]
        public void TestLoadSettingsFromConfigAutomation()
        {
            SharedSettingsModel expectedSharedSettings = new()
            {
                WebhookURL = "https://thisisatest.com/",
                RecipientId = "test",
                BaseURL = "https://localhost/api",
                Credentials = "Basic TestCreds",
                AuthPayloadLocation = "C:\\Server Status Site\\Payload\\Authorise.json",
                RefreshTime = 5
            };

            SharedSettingsModel result = SharedSettingsLoader.LoadSettingsFromConfig(SharedSettingsLoader.LoadConfig(Path.Combine(
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")),
                @"Mocks\Configs\AutomationTest.config")));

            result.Should().BeEquivalentTo(expectedSharedSettings);
        }

        /// <summary>
        /// Checks the LoadSettingsFromConfig method outputs all settings in the config.
        /// </summary>
        [TestMethod]
        public void TestLoadSettingsFromConfigReporter()
        {
            SharedSettingsModel expectedSharedSettings = new()
            {
                BaseURL = "https://localhost/api",
                Credentials = "Basic TestCreds",
                AuthPayloadLocation = "C:\\Server Status Site\\Payload\\Authorise.json",
                RefreshTime = 5
            };

            SharedSettingsModel result = SharedSettingsLoader.LoadSettingsFromConfig(SharedSettingsLoader.LoadConfig(Path.Combine(
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")),
                @"Mocks\Configs\ReporterTest.config")));

            result.Should().BeEquivalentTo(expectedSharedSettings);
        }
    }
}
