namespace DokiFS;

internal class ReadOnlyStream(Stream baseStream) : Stream
{
    readonly Stream innerStream = baseStream;

    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override bool CanSeek => innerStream.CanSeek;
    public override long Length => innerStream.Length;
    public override long Position
    {
        get => innerStream.Position;
        set
        {
            if (!CanSeek)
                throw new NotSupportedException("Stream does not support seeking");
            innerStream.Position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Cannot write to a read-only stream");
    public override void Flush() => throw new NotSupportedException("Cannot flush a read-only stream");
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (CanSeek == false) throw new NotSupportedException("Stream does not support seeking");
        return innerStream.Seek(offset, origin);
    }
    public override void SetLength(long value) => throw new NotSupportedException("Cannot write to a read-only stream");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            innerStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
