using System.Collections.Concurrent;
using DokiFS.Backends;
using DokiFS.Exceptions;
using DokiFS.Interfaces;

namespace DokiFS;

public class VirtualFileSystem : IVirtualFileSystem
{
    readonly ConcurrentDictionary<VPath, IFileSystemBackend> mounts = new();
    readonly object mountLock = new();

    // Mount Management
    public void Mount(VPath mountPoint, IFileSystemBackend backend)
        => Mount(mountPoint, backend, false);

    public void Mount(VPath mountPoint, IFileSystemBackend backend, bool force)
    {
        ArgumentNullException.ThrowIfNull(backend, nameof(backend));

        // Check mount point format
        if (mountPoint.StartsWith("/") == false)
        {
            throw new ArgumentException("Mount points should start with '/'");
        }

        lock (mountLock)
        {
            // Check if mount point is already in use
            if (mounts.TryGetValue(mountPoint, out IFileSystemBackend existingBackend))
            {
                throw new MountPointConflictException(mountPoint, "Mount point is already in use");
            }

            // Check with the backend if it will be mounted
            MountResult result = backend.OnMount(mountPoint);

            // The backend refused the mount and will not be forced
            if (result != MountResult.Accepted && force == false)
            {
                throw new MountRefusedException(result);
            }
            // If the result is accepted, or force is true, mount the backend
            else if (result == MountResult.Accepted || (result != MountResult.Accepted && force))
            {
                // This should not happen, but throw a generic exception
                if (mounts.TryAdd(mountPoint, backend) == false)
                {
                    throw new VfsException("Unable to add mount due to unspecified error");
                }
            }
        }
    }

    public void Unmount(VPath mountPoint)
        => Unmount(mountPoint, false);

    public void Unmount(VPath mountPoint, bool force)
    {
        lock (mountLock)
        {
            // Check if there actually is something mounted at the mount point
            if (mounts.TryGetValue(mountPoint, out IFileSystemBackend backend) == false)
            {
                throw new MountPointConflictException(mountPoint, $"There is already a backend present at mount point {mountPoint}");
            }

            // Check with the backend if it's ready to be unmounted
            UnmountResult result = backend.OnUnmount();

            // The backend refused the unmount and will not be forced
            if (result != UnmountResult.Accepted && force == false)
            {
                throw new UnmountRefusedException(result);
            }
            // If the result is accepted, or force is true, unmount the backend
            else if (result == UnmountResult.Accepted || (result != UnmountResult.Accepted && force))
            {
                KeyValuePair<VPath, IFileSystemBackend> mnt = mounts.FirstOrDefault(m => m.Key == mountPoint);
                if (mounts.TryRemove(mnt) == false)
                {
                    throw new VfsException("Unable to remove mount due to unspecified error");
                }
            }
        }
    }

    public bool IsMounted(VPath mountPoint) => mounts.ContainsKey(mountPoint);

    public bool TryGetMountedBackend(VPath path, out IFileSystemBackend backend)
    {
        // Try exact match
        if (mounts.TryGetValue(path, out backend))
        {
            return true;
        }

        // Get the sorted mount, for cases with overlap. For example /mnt and /mnt/a.
        // Both are valid, but /mnt/a has precedence in regards to mountpoints
        List<KeyValuePair<VPath, IFileSystemBackend>> currentMounts;
        lock (mountLock) { currentMounts = GetSortedMounts(); }

        foreach (KeyValuePair<VPath, IFileSystemBackend> curMount in currentMounts)
        {
            if (path.StartsWith(curMount.Key + VPath.DirectorySeparatorString))
            {
                backend = curMount.Value;
                return true;
            }
        }

        return false;
    }

    public IEnumerable<KeyValuePair<VPath, IFileSystemBackend>> GetMountPoints() => mounts.AsReadOnly();

    // Queries
    public bool Exists(VPath path) => throw new NotImplementedException();
    public IVfsEntry GetInfo(VPath path) => throw new NotImplementedException();
    public IEnumerable<IVfsEntry> ListDirectory(VPath path) => throw new NotImplementedException();

    // File Operations
    public void CreateFile(VPath path, long size = 0) => throw new NotImplementedException();
    public void DeleteFile(VPath path) => throw new NotImplementedException();
    public void MoveFile(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException();
    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite) => throw new NotImplementedException();
    public void CopyFile(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException();
    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite) => throw new NotImplementedException();

    // Filestreams
    public Stream OpenRead(VPath path) => throw new NotImplementedException();
    public Stream OpenWrite(VPath path) => throw new NotImplementedException();
    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share) => throw new NotImplementedException();

    // Directory Operations
    public void CreateDirectory(VPath path) => throw new NotImplementedException();
    public void DeleteDirectory(VPath path) => throw new NotImplementedException();
    public void DeleteDirectory(VPath path, bool recursive) => throw new NotImplementedException();
    public void MoveDirectory(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException();
    public void CopyDirectory(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException();

    List<KeyValuePair<VPath, IFileSystemBackend>> GetSortedMounts() => [..mounts
                .OrderByDescending(kvp => kvp.Key.FullPath.Length)
                .ThenBy(kvp => kvp.Key.FullPath, StringComparer.Ordinal)];
}
