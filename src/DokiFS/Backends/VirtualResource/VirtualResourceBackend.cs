using DokiFS.Interfaces;

namespace DokiFS.Backends.VirtualResource;

public class VirtualResourceBackend : IFileSystemBackend
{
    public BackendProperties BackendProperties => BackendProperties.Transient;

    readonly Dictionary<VPath, IVirtualResourceHandler> handlers = [];
    readonly Lock handlerLock = new();

    public void RegisterHandler<T>(VPath handlerPath)
        where T : IVirtualResourceHandler, new()
    {
        lock (handlerLock)
        {
            if (handlers.TryAdd(handlerPath, new T()) == false)
            {
                throw new InvalidOperationException("Handler is already registered.");
            }
        }
    }

    public void RegisterHandler(VPath handlerPath, IVirtualResourceHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (handlerLock)
        {
            if (handlers.TryAdd(handlerPath, handler) == false)
            {
                throw new InvalidOperationException("Handler is already registered.");
            }
        }
    }

    public bool UnregisterHandler(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        lock (handlerLock)
        {
            return handlers.Remove(methodName);
        }
    }

    public bool TryResolveHandler(VPath path,
        out IVirtualResourceHandler handler,
        out VPath pathRemainder)    // The remaining path, used as the parameters. Eg: /proc/meminfo -> /meminfo
    {
        string[] segments = path.Split();

        // This is probably root, root has no handler. Although some other methods should catch this
        // Since it's technically not part of the backend
        if (segments.Length == 0)
        {
            handler = null;
            pathRemainder = VPath.Empty;
            return false;
        }


        // Reassemble the path without the handler name
        if (segments.Length > 1)
        {
            pathRemainder = VPath.DirectorySeparatorString + string.Join(VPath.DirectorySeparator, segments[1..]);
        }
        else
        {
            pathRemainder = VPath.Root;
        }

        string handlerName = segments[0];
        return handlers.TryGetValue($"/{handlerName}", out handler);
    }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    public bool Exists(VPath path)
    {
        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            return handler.HandleExist(pathRemainder);
        }

        return false;
    }

    public IVfsEntry GetInfo(VPath path)
    {
        if (path == VPath.Root)
        {
            return new VfsEntry(
                "/",
                VfsEntryType.Directory,
                VfsEntryProperties.Default)
            {
                Size = 0,
                LastWriteTime = DateTime.UtcNow,
                FromBackend = typeof(VirtualResourceBackend),
                Description = "Virtual resource backend root"
            };
        }

        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            return handler.HandleGetInfo(pathRemainder);
        }

        return null;
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder) == false)
        {
            throw new DirectoryNotFoundException();
        }

        if (path == "/")
        {
            List<IVfsEntry> entries = [];
            lock (handlerLock)
            {
                foreach (VPath handlerName in handlers.Keys)
                {
                    entries.Add(new VfsEntry(
                        handlerName,
                        VfsEntryType.Directory,
                        VfsEntryProperties.Readonly)
                    {
                        Size = 0,
                        LastWriteTime = DateTime.UtcNow,
                        FromBackend = typeof(VirtualResourceBackend),
                        Description = $"Virtual Resource: {handlerName}"
                    });
                }
            }

            return entries;
        }

        return handler.HandleListDirectory(pathRemainder);
    }

    public void CreateFile(VPath path, long size = 0)
        => throw new NotSupportedException();

    public void DeleteFile(VPath path)
        => throw new NotSupportedException();

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => throw new NotSupportedException();

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
        => throw new NotSupportedException();

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => throw new NotSupportedException();

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
        => throw new NotSupportedException();

    public Stream OpenRead(VPath path)
    {
        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            return handler.HandleOpenRead(pathRemainder);
        }

        throw new FileNotFoundException($"File not found: {path}");
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            return handler.HandleOpenWrite(pathRemainder, mode, access);
        }

        throw new FileNotFoundException($"File not found: {path}");
    }

    public void CreateDirectory(VPath path)
        => throw new NotSupportedException();

    public void DeleteDirectory(VPath path)
        => throw new NotSupportedException();

    public void DeleteDirectory(VPath path, bool recursive)
        => throw new NotSupportedException();

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
        => throw new NotSupportedException();

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
        => throw new NotSupportedException();

}
