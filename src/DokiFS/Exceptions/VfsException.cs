namespace DokiFS.Exceptions;

/// <summary>
/// Generic VFS Exception
/// </summary>
public class VfsException : IOException
{
    public VfsException() { }
    public VfsException(string message) : base(message) { }
    public VfsException(string message, Exception innerException) : base(message, innerException) { }
}
