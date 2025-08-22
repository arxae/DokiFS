using DokiFS.Backends.Memory.Nodes;

namespace DokiFS.Backends.Memory;

public class DetachedNodeException : Exception
{
    public MemoryNode Node { get; set; }

    public DetachedNodeException(MemoryNode node)
        : base($"Node is detached: {node.FullPath}")
    {
        Node = node;
    }
}
