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
        handlerPath = NormalizeHandlerPath(handlerPath);

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
        handlerPath = NormalizeHandlerPath(handlerPath);

        lock (handlerLock)
        {
            if (handlers.TryAdd(handlerPath, handler) == false)
            {
                throw new InvalidOperationException("Handler is already registered.");
            }
        }
    }

    public bool UnregisterHandler(VPath handlerPath)
    {
        handlerPath = NormalizeHandlerPath(handlerPath);

        lock (handlerLock)
        {
            return handlers.Remove(handlerPath);
        }
    }

    static VPath NormalizeHandlerPath(VPath handlerPath)
    {
        if (handlerPath == VPath.Root)
        {
            throw new ArgumentException("Handler cannot be root.", nameof(handlerPath));
        }

        string raw = handlerPath.ToString();
        if (raw.StartsWith(VPath.DirectorySeparatorString, StringComparison.Ordinal) == false)
        {
            raw = VPath.DirectorySeparatorString + raw;
        }

        return raw;
    }

    public bool TryResolveHandler(VPath path,
        out IVirtualResourceHandler handler,
        out VPath pathRemainder)
    {
        handler = null;
        pathRemainder = VPath.Empty;

        if (path == VPath.Root)
            return false;

        string[] segments = path.Split();
        if (segments.Length == 0)
            return false;

        string handlerName = segments[0];
        pathRemainder = segments.Length > 1
            ? VPath.DirectorySeparatorString + string.Join(VPath.DirectorySeparator, segments[1..])
            : VPath.Root;

        lock (handlerLock)
        {
            return handlers.TryGetValue("/" + handlerName, out handler);
        }
    }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    public bool Exists(VPath path)
    {
        if (path == VPath.Root) return true;

        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            if (handler.CanRead == false) return false;
            return handler.HandleExist(pathRemainder);
        }
        return false;
    }

    public IVfsEntry GetInfo(VPath path)
    {
        if (path == VPath.Root)
        {
            DateTime now = DateTime.UtcNow;
            return new VfsEntry(
                "/",
                VfsEntryType.Directory,
                VfsEntryProperties.None)
            {
                Size = 0,
                LastWriteTime = now,
                FromBackend = typeof(VirtualResourceBackend),
                Description = "Virtual resource backend root"
            };
        }

        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder))
        {
            if (handler.CanRead == false) return null;
            return handler.HandleGetInfo(pathRemainder);
        }

        return null;
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        if (path == VPath.Root)
        {
            DateTime now = DateTime.UtcNow;
            List<IVfsEntry> entries;
            lock (handlerLock)
            {
                entries = [.. handlers.Keys.Select(handlerPath =>
                    new VfsEntry(
                        handlerPath,
                        VfsEntryType.Directory,
                        VfsEntryProperties.Readonly)
                    {
                        Size = 0,
                        LastWriteTime = now,
                        FromBackend = typeof(VirtualResourceBackend),
                        Description = $"Virtual Resource: {handlerPath}"
                    }).Cast<IVfsEntry>()];
            }
            return entries;
        }

        if (TryResolveHandler(path, out IVirtualResourceHandler handler, out VPath pathRemainder) == false)
        {
            throw new DirectoryNotFoundException(path.ToString());
        }

        if (handler.CanRead == false) return [];

        return handler.HandleListDirectory(pathRemainder) ?? [];
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
            if (handler.CanRead == false) throw new NotAllowedToReadException();
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
            if (handler.CanWrite == false) throw new NotAllowedToWriteException();
            return handler.HandleOpenWrite(pathRemainder, mode, access, share);
        }
        throw new FileNotFoundException($"File not found: {path}");
    }

    public void CreateDirectory(VPath path) => throw new NotSupportedException();
    public void DeleteDirectory(VPath path) => throw new NotSupportedException();
    public void DeleteDirectory(VPath path, bool recursive) => throw new NotSupportedException();
    public void MoveDirectory(VPath sourcePath, VPath destinationPath) => throw new NotSupportedException();
    public void CopyDirectory(VPath sourcePath, VPath destinationPath) => throw new NotSupportedException();
}
