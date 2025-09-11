namespace DokiFS.Backends.Memory.Nodes;

public class MemoryFileNode : MemoryNode, IDisposable
{
    byte[] content;
    readonly Lock contentLock = new();
    bool disposed;

    public override long Size
    {
        get
        {
            lock (contentLock) return content?.LongLength ?? 0;
        }
    }

    public MemoryFileNode(string filePath)
    {
        EntryType = Interfaces.VfsEntryType.File;
        FullPath = new VPath(filePath);
        FromBackend = typeof(MemoryFileSystemBackend);
        Description = "Memory File";
    }

    public override Stream OpenRead()
    {
        ObjectDisposedException.ThrowIf(disposed, nameof(MemoryFileNode));

        lock (contentLock)
        {
            return new MemoryStream(content ?? [], false);
        }
    }

    public override Stream OpenWrite(
        FileMode mode = FileMode.OpenOrCreate,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.Read)
    {
        ObjectDisposedException.ThrowIf(disposed, nameof(MemoryFileNode));

        if (access is FileAccess.Read)
            throw new NotSupportedException("Write requires FileAccess.Write or ReadWrite.");

        lock (contentLock)
        {
            byte[] seed = [];
            bool appendPosition = false;

            switch (mode)
            {
                case FileMode.CreateNew:
                    if (content != null)
                        throw new IOException("File already exists.");
                    content = [];
                    break;

                case FileMode.Create:
                    content = [];
                    break;

                case FileMode.Open:
                    if (content == null)
                        throw new FileNotFoundException("File does not exist.", FullPath.ToString());
                    seed = content;
                    break;

                case FileMode.OpenOrCreate:
                    content ??= [];
                    seed = content;
                    break;

                case FileMode.Truncate:
                    if (content == null)
                        throw new FileNotFoundException("File does not exist.", FullPath.ToString());
                    content = [];
                    break;

                case FileMode.Append:
                    content ??= [];
                    seed = content;
                    appendPosition = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return new MemoryFileWriteStream(this, seed, appendPosition);
        }
    }

    public void ClearContent()
    {
        ObjectDisposedException.ThrowIf(disposed, nameof(MemoryFileNode));
        lock (contentLock)
        {
            content = null;
            LastWriteTime = DateTime.UtcNow;
        }
    }

    public override MemoryFileNode Clone()
    {
        lock (contentLock)
        {
            MemoryFileNode clone = new(FullPath.GetLeaf());
            CopyCommonStateTo(clone);

            if (content is { Length: > 0 })
            {
                clone.content = (byte[])content.Clone();
            }
            return clone;
        }
    }

    internal void SetSize(long size)
    {
        ObjectDisposedException.ThrowIf(disposed, nameof(MemoryFileNode));

        ArgumentOutOfRangeException.ThrowIfNegative(size);

        lock (contentLock)
        {
            if (size == 0)
            {
                content = [];
            }
            else
            {
                content = new byte[size];
            }
            LastWriteTime = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
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
        readonly MemoryFileNode owner;

        public MemoryFileWriteStream(MemoryFileNode owner, byte[] seed, bool appendAtEnd)
        {
            this.owner = owner;

            if (seed.Length > 0)
            {
                Write(seed, 0, seed.Length);
                if (!appendAtEnd)
                {
                    Position = 0; // overwrite semantics
                }
            }

            if (appendAtEnd)
            {
                Position = Length;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (owner.contentLock)
                {
                    owner.content = ToArray();
                    owner.LastWriteTime = DateTime.UtcNow;
                }
            }
            base.Dispose(disposing);
        }
    }
}

