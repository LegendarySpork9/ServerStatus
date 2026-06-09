// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusReporter.Abstractions
{
    /// <summary>
    /// Interface for the TCP Client.
    /// </summary>
    public interface ITCPClient
    {
        Task<bool> PingAddress(string ipAddress, int port);
    }
}
