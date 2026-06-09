// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the logger service.
    /// </summary>
    public interface ILoggerService
    {
        void ChangeIdentifier(string value);
        void LogMessage(string level, string message);
    }
}
