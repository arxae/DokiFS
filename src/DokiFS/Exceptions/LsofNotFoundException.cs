namespace DokiFS.Exceptions;

public class LsofNotFoundException : Exception
{
    public LsofNotFoundException()
        : base("The 'lsof' command is not available on this system. This command is required to check if files are in use.") { }
}
