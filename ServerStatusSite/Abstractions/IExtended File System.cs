// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;

namespace ServerStatusSite.Abstractions
{
    /// <summary>
    /// Interface for extended file system operations.
    /// </summary>
    public interface IExtendedFileSystem : IFileSystem
    {
        Task WriteAllText(string path, string contents);
    }
}
