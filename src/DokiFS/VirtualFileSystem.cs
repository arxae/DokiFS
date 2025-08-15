using System.Collections.Concurrent;
using DokiFS.Backends;
using DokiFS.Exceptions;
using DokiFS.Interfaces;

namespace DokiFS;

public class VirtualFileSystem : IVirtualFileSystem
{
    readonly ConcurrentDictionary<VPath, IFileSystemBackend> mounts = new();
    readonly Lock mountLock = new();

    // Mount Management
    public void Mount(VPath mountPoint, IFileSystemBackend backend)
        => Mount(mountPoint, backend, false);

    public void Mount(VPath mountPoint, IFileSystemBackend backend, bool force)
    {
        ArgumentNullException.ThrowIfNull(backend);

        // Check mount point format
        if (mountPoint.StartsWith("/") == false)
        {
            throw new ArgumentException("Mount points should start with '/'");
        }

        lock (mountLock)
        {
            // Check if mount point is already in use
            if (mounts.TryGetValue(mountPoint, out IFileSystemBackend _))
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
            else if ((result == MountResult.Accepted || force)
                && (mounts.TryAdd(mountPoint, backend) == false))
            {
                // This should not happen, but throw a generic exception
                throw new VfsException("Unable to add mount due to unspecified error");
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

    public bool TryGetMountedBackend(VPath path, out IFileSystemBackend backend, out VPath backendPath)
    {
        // Try exact match
        if (mounts.TryGetValue(path, out backend))
        {
            backendPath = VPath.Root;
            return true;
        }

        // Get the sorted mount, for cases with overlap. For example /mnt and /mnt/a.
        // Both are valid, but /mnt/a has precedence in regards to mountpoints
        List<KeyValuePair<VPath, IFileSystemBackend>> currentMounts;
        lock (mountLock) { currentMounts = GetSortedMounts(); }

        foreach (KeyValuePair<VPath, IFileSystemBackend> currMount in currentMounts)
        {
            if (currMount.Key.IsRoot) continue;

            // Check if the path starts with the mount point followed by either nothing or a separator
            if (path.StartsWith(currMount.Key) &&
                (path.Length == currMount.Key.Length
                || path.FullPath[currMount.Key.Length] == '/'))
            {
                backend = currMount.Value;
                backendPath = path.ReduceStart(currMount.Key);
                return true;
            }
        }

        if (mounts.TryGetValue("/", out backend))
        {
            backendPath = VPath.Root;
            return true;
        }

        backendPath = VPath.Empty;
        return false;
    }

    public IEnumerable<KeyValuePair<VPath, IFileSystemBackend>> GetMountPoints() => mounts.AsReadOnly();

    // Queries
    public bool Exists(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath))
        {
            return backend.Exists(backendPath);
        }

        throw new BackendNotFoundException(path, nameof(Exists));
    }

    public IVfsEntry GetInfo(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath))
        {
            return backend.GetInfo(backendPath);
        }

        throw new BackendNotFoundException(path, nameof(GetInfo));
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath))
        {
            return backend.ListDirectory(backendPath);
        }

        throw new BackendNotFoundException(path, nameof(ListDirectory));
    }

    // File Operations
    public void CreateFile(VPath path, long size = 0)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath) == false)
        {
            throw new BackendNotFoundException(path, nameof(CreateFile));
        }

        backend.CreateFile(backendPath, size);
    }

    public void DeleteFile(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath) == false)
        {
            throw new BackendNotFoundException(path, nameof(DeleteFile));
        }

        backend.DeleteFile(backendPath);
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, false);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        bool foundSourceBackend = TryGetMountedBackend(sourcePath, out IFileSystemBackend sourceBackend, out VPath sourceBackendPath);
        bool foundDestinationBackend = TryGetMountedBackend(destinationPath, out IFileSystemBackend destinationBackend, out VPath destinationBackendPath);

        if (foundSourceBackend == false)
        {
            throw new BackendNotFoundException(sourcePath, "File Move - Source");
        }

        if (foundDestinationBackend == false)
        {
            throw new BackendNotFoundException(destinationPath, "File Move - Destination");
        }

        // If source and destination are the same backend, we can use the normal backend operation
        if (ReferenceEquals(sourceBackend, destinationBackend))
        {
            sourceBackend.MoveFile(sourceBackendPath, destinationBackendPath, overwrite);
            return;
        }

        MoveCopyFileOperation(CopyMoveOperations.Move,
            sourceBackend, sourceBackendPath,
            destinationBackend, destinationBackendPath,
            overwrite);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, false);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        bool foundSourceBackend = TryGetMountedBackend(sourcePath, out IFileSystemBackend sourceBackend, out VPath sourceBackendPath);
        bool foundDestinationBackend = TryGetMountedBackend(destinationPath, out IFileSystemBackend destinationBackend, out VPath destinationBackendPath);

        if (foundSourceBackend == false)
        {
            throw new BackendNotFoundException(sourcePath, "File Copy - Source");
        }

        if (foundDestinationBackend == false)
        {
            throw new BackendNotFoundException(destinationPath, "File Copy - Destination");
        }

        // If source and destination are the same backend, we can use the normal backend operation
        if (ReferenceEquals(sourceBackend, destinationBackend))
        {
            sourceBackend.CopyFile(sourceBackendPath, destinationBackendPath, overwrite);
            return;
        }

        MoveCopyFileOperation(CopyMoveOperations.Copy,
            sourceBackend, sourceBackendPath,
            destinationBackend, destinationBackendPath,
            overwrite);
    }

    // Filestreams
    public Stream OpenRead(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath))
        {
            return backend.OpenRead(backendPath);
        }

        throw new BackendNotFoundException(path, nameof(OpenRead));
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath))
        {
            return backend.OpenWrite(backendPath, mode, access, share);
        }

        throw new BackendNotFoundException(path, nameof(OpenWrite));
    }

    // Directory Operations
    public void CreateDirectory(VPath path)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath) == false)
        {
            throw new BackendNotFoundException(path, nameof(CreateDirectory));
        }

        backend.CreateDirectory(backendPath);
    }

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        if (TryGetMountedBackend(path, out IFileSystemBackend backend, out VPath backendPath) == false)
        {
            throw new BackendNotFoundException(path, nameof(DeleteDirectory));
        }

        backend.DeleteDirectory(backendPath, recursive);
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
        => MoveCopyDirectoryOperation(CopyMoveOperations.Move, sourcePath, destinationPath);

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
        => MoveCopyDirectoryOperation(CopyMoveOperations.Copy, sourcePath, destinationPath);

    List<KeyValuePair<VPath, IFileSystemBackend>> GetSortedMounts() => [..mounts
        .OrderByDescending(kvp => kvp.Key.FullPath.Length)
        .ThenBy(kvp => kvp.Key.FullPath, StringComparer.Ordinal)];

    enum CopyMoveOperations { Copy, Move }
    static void MoveCopyFileOperation(CopyMoveOperations op,
        IFileSystemBackend sourceBackend, VPath sourceBackendPath,
        IFileSystemBackend destinationBackend, VPath destinationBackendPath,
        bool overwrite)
    {
        // Different backends, stream the file from one to the other
        using Stream sourceStream = sourceBackend.OpenRead(sourceBackendPath);
        FileMode writeMode = overwrite ? FileMode.Create : FileMode.CreateNew;
        using Stream destStream = destinationBackend.OpenWrite(destinationBackendPath, writeMode, FileAccess.Write, FileShare.None);

        const int bufferSize = 81920;
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        long totalBytesCopied = 0;
        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            destStream.Write(buffer, 0, bytesRead);
            totalBytesCopied += bytesRead;
        }

        destStream.Flush();

        // Verify length
        if (sourceStream.Length != totalBytesCopied)
        {
            throw new IOException($"File copy failed: expected {sourceStream.Length} bytes but copied {totalBytesCopied} bytes");
        }

        sourceStream.Dispose();
        destStream.Dispose();

        // If destination backend requires a commit, call it
        if (destinationBackend is ICommit commitBackend)
        {
            commitBackend.Commit();
        }

        // If we are moving the file, delete it from the source backend
        if (op == CopyMoveOperations.Move)
        {
            try
            {
                sourceBackend.DeleteFile(sourceBackendPath);
            }
            catch (Exception deleteEx)
            {
                // This leaves the system in an inconsistent state (file copied but not moved)
                throw new VfsException($"Failed to delete source file '{sourceBackendPath}' after successful transfer during move operation. Destination '{destinationBackendPath}' may exist.", deleteEx);
            }
        }
    }

    void MoveCopyDirectoryOperation(CopyMoveOperations op, VPath sourcePath, VPath destinationPath)
    {
        bool foundSourceBackend = TryGetMountedBackend(sourcePath, out IFileSystemBackend sourceBackend, out VPath sourceBackendPath);
        bool foundDestinationBackend = TryGetMountedBackend(destinationPath, out IFileSystemBackend destinationBackend, out VPath destinationBackendPath);

        if (foundSourceBackend == false && foundDestinationBackend == false)
        {
            throw new BackendNotFoundException(sourcePath, $"File {op}");
        }

        // If source and destination are the same backend, we can use the backend's native operation
        if (ReferenceEquals(sourceBackend, destinationBackend))
        {
            if (op == CopyMoveOperations.Copy)
            {
                sourceBackend.CopyDirectory(sourceBackendPath, destinationBackendPath);
            }
            else
            {
                sourceBackend.MoveDirectory(sourceBackendPath, destinationBackendPath);
            }
            return;
        }

        // Different backends, we need to copy the directory structure manually. Check if destination exists
        try
        {
            destinationBackend.Exists(destinationBackendPath);
        }
        catch (Exception) { /* Ignore errors and assume it doesn't exist */ }

        // Create the destination directory
        destinationBackend.CreateDirectory(destinationBackendPath);

        // Check if source directory exists
        if (sourceBackend.Exists(sourceBackendPath) == false)
        {
            throw new DirectoryNotFoundException($"Source directory '{sourcePath}' not found.");
        }

        // Get source directory info
        IVfsEntry sourceInfo = sourceBackend.GetInfo(sourceBackendPath);
        if (sourceInfo.EntryType == VfsEntryType.File)
        {
            throw new IOException($"Source path '{sourcePath}' is not a directory.");
        }

        IEnumerable<IVfsEntry> entries = GatherEntries(sourceBackend, sourceBackendPath);

        foreach (IVfsEntry entry in entries)
        {
            if (entry.EntryType == VfsEntryType.Directory)
            {
                destinationBackend.CreateDirectory(entry.FullPath);
            }
            else if (entry.EntryType == VfsEntryType.File)
            {
                MoveCopyFileOperation(op, sourceBackend, entry.FullPath, destinationBackend, entry.FullPath, true);
            }
        }
    }

    static IEnumerable<IVfsEntry> GatherEntries(IFileSystemBackend backend, VPath backendPath)
    {
        HashSet<IVfsEntry> entries = [];
        Queue<VPath> directoriesToProcess = [];
        directoriesToProcess.Enqueue(backendPath);

        while (directoriesToProcess.Count > 0)
        {
            VPath currentDir = directoriesToProcess.Dequeue();

            // Get all subdirectories in the current directory
            List<IVfsEntry> subdirectories = [.. backend.ListDirectory(currentDir)];

            // Add subdirectories to our results list
            foreach (IVfsEntry entry in subdirectories)
            {
                entries.Add(entry);
            }

            // Queue subdirectories for processing
            foreach (IVfsEntry entry in subdirectories)
            {
                if (entry.EntryType == VfsEntryType.File) continue;
                directoriesToProcess.Enqueue(entry.FullPath);
            }
        }

        // Only add the original path if it doesn't have any subdirectories
        if (entries.Count == 0)
        {
            entries.Add(backend.GetInfo(backendPath));
        }

        // Return directories first, then files
        // Longest directory first
        return entries.OrderByDescending(e => e.EntryType).ThenBy(e => e.FullPath.Length);
    }
}
