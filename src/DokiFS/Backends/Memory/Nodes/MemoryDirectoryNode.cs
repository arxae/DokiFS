namespace DokiFS.Backends.Memory.Nodes;

public class MemoryDirectoryNode : MemoryNode, IDisposable
{
    public List<MemoryNode> Children { get; } = [];

    public MemoryDirectoryNode(VPath path)
    {
        FullPath = path;
        Parent = null;

        EntryType = Interfaces.VfsEntryType.Directory;
        FromBackend = typeof(MemoryFileSystemBackend);
        Description = "Memory Directory";
    }

    public void AddChild(MemoryNode child)
    {
        child.Parent = this;

        // Recalculate the path for the child
        child.FullPath = FullPath.Append(child.FullPath);

        // if the child has any children and is a folder, recalculate that path as well
        if (child is MemoryDirectoryNode dirNode)
        {
            foreach (MemoryNode grandchild in dirNode.Children)
            {
                grandchild.FullPath = dirNode.FullPath.Append(grandchild.FullPath.GetLeaf());
            }
        }

        Children.Add(child);
    }

    public void RemoveChild(MemoryNode child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;

            // If the child is a directory, also update its children
            if (child is MemoryDirectoryNode dirNode)
            {
                foreach (MemoryNode grandchild in dirNode.Children)
                {
                    grandchild.Parent = null;
                }
            }
        }
    }

    public bool HasDirectoryChildren() => Children.Any(c => c.GetType() == typeof(MemoryDirectoryNode));

    public override MemoryDirectoryNode Clone() => (MemoryDirectoryNode)MemberwiseClone();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (MemoryNode child in Children)
        {
            if (child is IDisposable disposableChild)
            {
                disposableChild.Dispose();
            }
        }
    }
}
