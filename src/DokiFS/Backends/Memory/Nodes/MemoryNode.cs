namespace DokiFS.Backends.Memory.Nodes;

public abstract class MemoryNode : VfsEntry
{
    public MemoryNode Parent { get; set; }

    public abstract MemoryNode Clone();
}
