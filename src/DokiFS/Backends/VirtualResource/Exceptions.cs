namespace DokiFS.Backends.VirtualResource;

public class NotAllowedToReadException : Exception
{
    public NotAllowedToReadException()
        : base("This handler is not allowed to read.")
    {
    }
}

public class NotAllowedToWriteException : Exception
{
    public NotAllowedToWriteException()
        : base("This handler is not allowed to write.")
    {
    }
}
