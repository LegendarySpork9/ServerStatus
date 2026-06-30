// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Abstractions;

namespace ServerStatusCommon.Implementations
{
    public class FileSystemWrapper : IFileSystem
    {
        /// <summary>
        /// Returns the text in a given file.
        /// </summary>
        public Task<string> ReadAllText(string path) => File.ReadAllTextAsync(path);

        /// <summary>
        /// Returns whether the file exists for a given path.
        /// </summary>
        public bool FileExists(string path) => File.Exists(path);
    }
}
