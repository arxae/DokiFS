namespace DokiFS.Backends.Memory.Nodes;

public class MemoryDirectoryNode : MemoryNode, IDisposable
{
    // Name (leaf) -> node
    private readonly Dictionary<string, MemoryNode> _children = new(StringComparer.Ordinal);

    bool disposed;
    readonly Lock sync = new();

    public IReadOnlyCollection<MemoryNode> Children
    {
        get
        {
            lock (sync)
            {
                return [.. _children.Values];
            }
        }
    }

    public int ChildCount
    {
        get
        {
            lock (sync) return _children.Count;
        }
    }

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
        ArgumentNullException.ThrowIfNull(child);

        lock (sync)
        {
            string leaf = child.FullPath.GetLeaf();

            if (_children.ContainsKey(leaf))
                throw new IOException($"A node named '{leaf}' already exists in '{FullPath}'");

            child.Parent = this;
            child.FullPath = FullPath.Append(leaf);

            if (child is MemoryDirectoryNode dirNode)
            {
                RecalculateDescendantPaths(dirNode);
            }

            _children.Add(leaf, child);
        }
    }

    public void RemoveChild(MemoryNode child)
    {
        if (child == null) return;

        lock (sync)
        {
            string leaf = child.FullPath.GetLeaf();
            if (_children.Remove(leaf))
            {
                child.Parent = null;
            }
        }
    }

    public bool HasDirectoryChildren()
    {
        lock (sync)
        {
            return _children.Values.Any(c => c is MemoryDirectoryNode);
        }
    }

    public bool TryGetChild(string name, out MemoryNode node)
    {
        lock (sync)
        {
            return _children.TryGetValue(name, out node!);
        }
    }

    public override MemoryDirectoryNode Clone()
    {
        lock (sync)
        {
            MemoryDirectoryNode clone = new(FullPath);
            CopyCommonStateTo(clone);

            foreach (MemoryNode child in _children.Values)
            {
                MemoryNode childClone = child.Clone();
                string leaf = childClone.FullPath.GetLeaf();

                childClone.Parent = clone;
                childClone.FullPath = clone.FullPath.Append(leaf);

                if (childClone is MemoryDirectoryNode dirClone)
                {
                    RecalculateDescendantPaths(dirClone);
                }

                clone._children.Add(leaf, childClone);
            }

            return clone;
        }
    }

    static void RecalculateDescendantPaths(MemoryDirectoryNode directory)
    {
        // Lock each directory independently to avoid holding parent locks too long
        lock (directory.sync)
        {
            foreach (MemoryNode child in directory._children.Values)
            {
                child.FullPath = directory.FullPath.Append(child.FullPath.GetLeaf());
                if (child is MemoryDirectoryNode childDir)
                {
                    RecalculateDescendantPaths(childDir);
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed == false)
        {
            if (disposing)
            {
                List<MemoryNode> snapshot;
                lock (sync)
                {
                    snapshot = [.. _children.Values];
                    _children.Clear();
                }

                foreach (MemoryNode child in snapshot)
                {
                    if (child is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
            disposed = true;
        }
    }
}
