namespace DokiFS.Backends.Journal;

/// <summary>
/// A stream implementation that captures all write operations and records them in the journal
/// </summary>
internal sealed class JournalCapturingStream : Stream
{
    readonly JournalFileSystemBackend backend;
    readonly VPath path;
    readonly FileMode mode;
    readonly FileAccess access;
    readonly FileShare share;
    readonly MemoryStream buffer;
    bool disposed;

    internal JournalCapturingStream(
        VPath path,
        JournalFileSystemBackend backend,
        FileMode mode,
        FileAccess access,
        FileShare share,
        byte[] baseline)
    {
        this.path = path;
        this.backend = backend;
        this.mode = mode;
        this.access = access;
        this.share = share;

        buffer = new MemoryStream();

        if (baseline.Length > 0)
        {
            buffer.Write(baseline, 0, baseline.Length);
            if (mode == FileMode.Append)
            {
                // Position at end for Append
                buffer.Position = buffer.Length;
            }
            else
            {
                buffer.Position = 0;
            }
        }
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => buffer.Length;

    public override long Position
    {
        get => buffer.Position;
        set => buffer.Position = value;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => buffer.Seek(offset, origin);

    public override void SetLength(long value)
        => buffer.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => this.buffer.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer)
        => this.buffer.Write(buffer);

    protected override void Dispose(bool disposing)
    {
        if (disposed == false && disposing)
        {
            byte[] data = buffer.ToArray();
            backend.RecordCompletedWrite(path, mode, access, share, data);
            buffer.Dispose();
        }
        disposed = true;
        base.Dispose(disposing);
    }
}
