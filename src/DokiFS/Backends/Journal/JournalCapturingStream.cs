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
    readonly MemoryStream internalBuffer;
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

        internalBuffer = new MemoryStream();

        if (baseline.Length > 0)
        {
            internalBuffer.Write(baseline, 0, baseline.Length);
            // Position at end for Append
            internalBuffer.Position = mode == FileMode.Append
                ? internalBuffer.Length
                : 0;
        }
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => internalBuffer.Length;

    public override long Position
    {
        get => internalBuffer.Position;
        set => internalBuffer.Position = value;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => internalBuffer.Seek(offset, origin);

    public override void SetLength(long value)
        => internalBuffer.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => this.internalBuffer.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer)
        => this.internalBuffer.Write(buffer);

    protected override void Dispose(bool disposing)
    {
        if (disposed == false && disposing)
        {
            byte[] data = internalBuffer.ToArray();
            backend.RecordCompletedWrite(path, mode, access, share, data);
            internalBuffer.Dispose();
        }
        disposed = true;
        base.Dispose(disposing);
    }
}
