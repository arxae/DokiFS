using System.Diagnostics.CodeAnalysis;

namespace DokiFS.Interfaces;

public interface IVirtualFileSystem : IVfsOperations
{
    void Mount(VPath mountPoint, IFileSystemBackend backend);
    void Mount(VPath mountPoint, IFileSystemBackend backend, bool force);
    void Unmount(VPath mountPoint);
    void Unmount(VPath mountPoint, bool force);
    bool IsMounted(VPath mountPoint);
    bool TryGetMountedBackend(VPath path, [MaybeNullWhen(false)] out IFileSystemBackend backend, [MaybeNullWhen(false)] out VPath backendPath);
    IEnumerable<KeyValuePair<VPath, IFileSystemBackend>> GetMountPoints();



    // TODO: Move to extension methods?
    // string GetTempFile(string basePath = "/temp");
    // string GetTempDirectory(string basePath = "/temp");
}
