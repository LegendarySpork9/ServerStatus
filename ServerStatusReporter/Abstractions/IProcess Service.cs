// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusReporter.Abstractions
{
    /// <summary>
    /// Interface for checking if a process is running.
    /// </summary>
    public interface IProcessService
    {
        bool IsRunning(int processId, DateTime expectedStartTime);
    }
}
