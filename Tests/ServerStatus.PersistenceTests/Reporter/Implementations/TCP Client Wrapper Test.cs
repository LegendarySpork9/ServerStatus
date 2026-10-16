// Copyright © - Unpublished - Toby Hunter
using Moq;
using ServerStatusCommon.Abstractions;
using ServerStatusReporter.Implementations;
using System.Net;
using System.Net.Sockets;

namespace ServerStatus.PersistenceTests.Reporter.Implementations
{
    [TestClass]
    public class TCPClientWrapperTest
    {
        private readonly Mock<ILoggerService> _MockLogger = new();

        /// <summary>
        /// Checks whether the PingAddress method returns true when a listener is active on the given port.
        /// </summary>
        [TestMethod]
        public async Task TestPingAddressReturnsTrueWhenListenerActive()
        {
            TCPClientWrapper wrapper = new(_MockLogger.Object);

            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                bool result = await wrapper.PingAddress(
                    "127.0.0.1",
                    port);

                Assert.IsTrue(result);
            }

            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// Checks whether the PingAddress method returns false when no listener is active on the given port.
        /// </summary>
        [TestMethod]
        public async Task TestPingAddressReturnsFalseWhenNoListener()
        {
            TCPClientWrapper wrapper = new(_MockLogger.Object);

            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            bool result = await wrapper.PingAddress(
                "127.0.0.1",
                port);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks whether the PingAddress method returns false for an unreachable address.
        /// </summary>
        [TestMethod]
        public async Task TestPingAddressReturnsFalseForUnreachableAddress()
        {
            TCPClientWrapper wrapper = new(_MockLogger.Object);

            bool result = await wrapper.PingAddress(
                "192.0.2.1",
                1);

            Assert.IsFalse(result);
        }
    }
}
