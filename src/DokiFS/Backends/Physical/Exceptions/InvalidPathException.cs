namespace DokiFS.Backends.Physical.Exceptions;

public class InvalidPathException : ArgumentException
{
    public InvalidPathException() { }
    public InvalidPathException(string message) : base(message) { }
    public InvalidPathException(string message, Exception innerException) : base(message, innerException) { }
}
