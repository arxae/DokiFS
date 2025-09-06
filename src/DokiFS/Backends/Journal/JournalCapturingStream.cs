namespace DokiFS.Backends.Journal;

/// <summary>
/// A stream implementation that captures all write operations and records them in the journal
/// </summary>
internal sealed class JournalCapturingStream : Stream
{
    readonly VPath path;
    readonly JournalFileSystemBackend backend;
    readonly MemoryStream buffer;
    long position;
    bool disposed;

    public JournalCapturingStream(VPath path, JournalFileSystemBackend backend)
    {
        this.path = path;
        this.backend = backend;
        buffer = new MemoryStream();
        position = 0;
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => buffer.Length;

    public override long Position
    {
        get => position;
        set
        {
            position = value;
            if (buffer.Length < value)
            {
                buffer.SetLength(value);
            }
        }
    }

    public override void Flush() => buffer.Flush();

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
                Position = 0;
                break;
        }
        return Position;
    }

    public override void SetLength(long value) => buffer.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Ensure buffer is large enough
        if (position + count > this.buffer.Length)
        {
            this.buffer.SetLength(position + count);
        }

        // Write to our internal buffer
        this.buffer.Position = position;
        this.buffer.Write(buffer, offset, count);

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
            buffer.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }
}
