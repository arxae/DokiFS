namespace DokiFS.Backends.Memory.Nodes;

public class MemoryFileNode : MemoryNode, IDisposable
{
    byte[] content;
    readonly Lock contentLock = new();
    bool disposed;

    public override long Size => content?.Length ?? 0;

    public MemoryFileNode(string filePath)
    {
        EntryType = Interfaces.VfsEntryType.File;
        FullPath = new VPath(filePath);
        FromBackend = typeof(MemoryFileSystemBackend);
        Description = "Memory File";
    }

    public override Stream OpenRead()
    {
        lock (contentLock)
        {
            return new MemoryStream(content, false);
        }
    }

    public override Stream OpenWrite(FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read)
    {
        lock (contentLock)
        {
            MemoryFileWriteStream stream = new(this);

            if (mode == FileMode.Append && content?.Length > 0)
            {
                stream.Write(content, 0, content.Length);
            }

            return stream;
        }
    }

    public void ClearContent()
    {
        lock (contentLock)
        {
            content = null;
        }
    }

    public override MemoryFileNode Clone() => (MemoryFileNode)MemberwiseClone();

    internal void SetSize(long size)
    {
        lock (contentLock)
        {
            if (size > 0)
            {
                content = new byte[size];
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed == false)
        {
            if (disposing)
            {
                lock (contentLock)
                {
                    content = null;
                }
            }

            disposed = true;
        }
    }

    sealed class MemoryFileWriteStream : MemoryStream
    {
        private readonly MemoryFileNode _owner;

        public MemoryFileWriteStream(MemoryFileNode owner)
        {
            _owner = owner;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_owner.contentLock)
                {
                    _owner.content = ToArray();
                }
            }
            base.Dispose(disposing);
        }
    }
}

