namespace DokiFS.Exceptions;

public class BackendNotFoundException : VfsException
{
    public VPath Path { get; set; }
    public string Operation { get; set; }

    public BackendNotFoundException(VPath path, string operation)
        : base($"No backend found for path '{path}' during operation '{operation}'")
    {
        Path = path;
        Operation = operation;
    }
}
