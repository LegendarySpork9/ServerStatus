// Copyright (c) - Unpublished - Toby Hunter
using ServerStatusSite.Functions;
using Microsoft.AspNetCore.Http;

namespace ServerStatus.Tests.Site.Functions
{
    [TestClass]
    public class IPAddressFunctionTest
    {

        /// <summary>
        /// Tests whether the FetchIpAddress method returns the IP from the CF-Connecting-IP header.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressCFConnectingIP()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["CF-Connecting-IP"] = "203.0.113.1";

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                "203.0.113.1",
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns the IP from the X-Forwarded-For header.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressXForwardedFor()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.5";

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                "198.51.100.5",
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns the first IP from a comma-separated X-Forwarded-For header.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressXForwardedForMultiple()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.5, 10.0.0.1, 172.16.0.1";

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                "198.51.100.5",
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method prefers CF-Connecting-IP over X-Forwarded-For.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressCFConnectingIPPriority()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["CF-Connecting-IP"] = "203.0.113.1";
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.5";

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                "203.0.113.1",
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns the remote IP address when no headers are present.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressRemoteIpAddress()
        {
            DefaultHttpContext context = new();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                "192.168.1.1",
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns an empty string when no IP information is available.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressNoIp()
        {
            DefaultHttpContext context = new();

            HttpContextAccessor accessor = new()
            {
                HttpContext = context
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                string.Empty,
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns an empty string when the accessor is null.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressNullAccessor()
        {
            string actual = IPAddressFunction.FetchIpAddress(null!);

            Assert.AreEqual(
                string.Empty,
                actual);
        }

        /// <summary>
        /// Tests whether the FetchIpAddress method returns an empty string when the HttpContext is null.
        /// </summary>
        [TestMethod]
        public void TestFetchIpAddressNullHttpContext()
        {
            HttpContextAccessor accessor = new()
            {
                HttpContext = null
            };

            string actual = IPAddressFunction.FetchIpAddress(accessor);

            Assert.AreEqual(
                string.Empty,
                actual);
        }

    }
}
