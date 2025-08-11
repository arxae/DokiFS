using DokiFS.Backends;

namespace DokiFS.Interfaces;

public interface IFileSystemBackend : IVfsOperations
{
    public BackendProperties BackendProperties { get; }
}
