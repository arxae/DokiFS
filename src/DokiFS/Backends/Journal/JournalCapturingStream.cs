namespace DokiFS.Backends.Journal;

/// <summary>
/// A stream implementation that captures all write operations and records them in the journal
/// </summary>
internal sealed class JournalCapturingStream : Stream
{
    readonly VPath path;
    readonly JournalFileSystemBackend backend;
    readonly MemoryStream internalBuffer;
    long position;
    bool disposed;

    public JournalCapturingStream(VPath path, JournalFileSystemBackend backend, FileMode mode)
    {
        this.path = path;
        this.backend = backend;
        internalBuffer = new MemoryStream();

        // For modes that preserve or open existing content, load it from the journal.
        if (mode is FileMode.Append or FileMode.Open or FileMode.OpenOrCreate)
        {
            byte[] initialData = backend.GetFileContent(path);
            if (initialData.Length > 0)
            {
                internalBuffer.Write(initialData, 0, initialData.Length);
            }
        }
        // For Truncate, Create, and CreateNew, we start with an empty buffer, which is the default.

        // Set the initial position based on the mode.
        if (mode == FileMode.Append)
        {
            position = internalBuffer.Length;
        }
        else
        {
            position = 0;
        }
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => internalBuffer.Length;

    public override long Position
    {
        get => position;
        set
        {
            position = value;
            if (internalBuffer.Length < value)
            {
                internalBuffer.SetLength(value);
            }
        }
    }

    public override void Flush() => internalBuffer.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Read operations not supported on write-only journal stream");

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
            default:
                throw new ArgumentException("Invalid seek origin", nameof(origin));
        }
        return Position;
    }

    public override void SetLength(long value) => internalBuffer.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Ensure buffer is large enough
        if (position + count > this.internalBuffer.Length)
        {
            this.internalBuffer.SetLength(position + count);
        }

        // Write to our internal buffer
        this.internalBuffer.Position = position;
        this.internalBuffer.Write(buffer, offset, count);

        // Record the write operation in the journal
        byte[] data = new byte[count];
        Array.Copy(buffer, offset, data, 0, count);
        backend.RecordStreamWrite(path, position, data);

        position += count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed == false && disposing)
        {
            backend.RecordStreamClose(path);
            internalBuffer.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }
}
