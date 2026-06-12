// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the retry service.
    /// </summary>
    public interface IRetryService
    {
        Task<T> ExecuteAsync<T>( Func<Task<T>> action, Func<T, bool> isSuccess, Func<Task>? onBeforeRetry, string operationName, int maxRetries = 4, int delaySeconds = 30);
    }
}
