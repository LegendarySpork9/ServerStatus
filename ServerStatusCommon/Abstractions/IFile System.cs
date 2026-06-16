// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the file system operations.
    /// </summary>
    public interface IFileSystem
    {
        Task<string> ReadAllText(string path);
        bool FileExists(string path);
    }
}
