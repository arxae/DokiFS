using DokiFS.Backends;

namespace DokiFS.Interfaces;

public interface IFileSystemBackend : IVfsOperations
{
    public BackendProperties BackendProperties { get; }

    public MountResult OnMount(VPath mountPoint);
    public UnmountResult OnUnmount();
}
