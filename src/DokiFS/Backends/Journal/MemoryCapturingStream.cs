namespace DokiFS.Backends.Journal;

sealed class MemoryCapturingStream : Stream
{
    readonly MemoryStream buffer;
    readonly JournalEntry entry;
    bool isDisposed;

    public MemoryCapturingStream(JournalEntry entry)
    {
        buffer = new MemoryStream();
        this.entry = entry;
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => !isDisposed;
    public override long Length => buffer.Length;
    public override long Position
    {
        get => buffer.Position;
        set => buffer.Position = value;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(MemoryCapturingStream));
        this.buffer.Write(buffer, offset, count);
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(MemoryCapturingStream));
        buffer.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(MemoryCapturingStream));
        return buffer.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(MemoryCapturingStream));
        buffer.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("This stream is write-only");

    protected override void Dispose(bool disposing)
    {
        if (!isDisposed && disposing)
        {
            // First capture the data
            buffer.Position = 0;
            entry.Data = buffer.ToArray();

            // Then mark as disposed
            isDisposed = true;

            // Finally dispose the buffer
            buffer.Dispose();
        }
        base.Dispose(disposing);
    }
}
