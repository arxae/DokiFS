namespace DokiFS.Backends.Memory.Nodes;

public abstract class MemoryNode : VfsEntry
{
    // Parent is managed by directory AddChild/RemoveChild logic
    public MemoryNode Parent { get; internal set; }

    public bool IsRoot => Parent == null;

    protected void CopyCommonStateTo(MemoryNode target)
    {
        target.Description = Description;
        target.LastWriteTime = LastWriteTime;
    }

    // Remove from tree (directory logic still responsible for dictionary removal).
    internal void Detach() => Parent = null;

    public abstract MemoryNode Clone();
}
