// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Implementations;
using ServerStatusSite.Abstractions;

namespace ServerStatusSite.Implementations
{
    public class ExtendedFileSystemWrapper : FileSystemWrapper, IExtendedFileSystem
    {
        /// <summary>
        /// Writes the given contents to a file at the specified path.
        /// </summary>
        public async Task WriteAllText(
            string path,
            string contents) => await File.WriteAllTextAsync(
            path,
            contents);
    }
}
