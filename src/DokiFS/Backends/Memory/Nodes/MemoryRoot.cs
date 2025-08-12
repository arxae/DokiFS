namespace DokiFS.Backends.Memory.Nodes;

public class MemoryRoot : MemoryDirectoryNode
{
    public MemoryRoot() : base("/")
    {
        Description = "Memory Root";
    }
}
