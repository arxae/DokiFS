using DokiFS.Backends.Memory.Nodes;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Memory;

public class MemoryFileSystemBackend : IFileSystemBackend, IDisposable
{
    public BackendProperties BackendProperties => BackendProperties.Transient;

    private readonly MemoryRoot root;
    private bool disposed;

    public MemoryFileSystemBackend()
    {
        root = new();
    }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    // Queries
    public bool Exists(VPath path) => TryGetNode(path, out MemoryNode _);

    public IVfsEntry GetInfo(VPath path)
    {
        if (TryGetNode(path, out MemoryNode node))
        {
            return node;
        }

        throw new FileNotFoundException($"Path not found within backend: '{path}'");
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        if (TryGetNode(path, out MemoryNode node))
        {
            if (node is MemoryDirectoryNode dirNode)
            {
                return dirNode.Children;
            }
            throw new InvalidOperationException($"Path '{path}' is not a directory.");
        }

        throw new DirectoryNotFoundException($"Path not found within backend: '{path}'");
    }

    // File Operations
    public void CreateFile(VPath path, long size = 0)
    {
        VPath parentDirectoryPath = path.GetDirectory();

        if (TryGetNode(parentDirectoryPath, out MemoryNode parentNode) == false)
        {
            CreateDirectory(parentDirectoryPath);
            if (TryGetNode(parentDirectoryPath, out parentNode) == false)
            {
                throw new DirectoryNotFoundException($"Could not create parent directory: '{parentDirectoryPath}'");
            }
        }

        if (parentNode is not MemoryDirectoryNode parentDirectory)
        {
            throw new IOException($"Parent path is not a directory: '{parentDirectoryPath}'");
        }

        if (TryGetNode(path, out _))
        {
            return;
        }

        MemoryFileNode fileNode = new(path.GetLeaf())
        {
            LastWriteTime = DateTime.UtcNow
        };
        fileNode.SetSize(size);
        parentDirectory.AddChild(fileNode);
    }

    public void DeleteFile(VPath path)
    {
        if (TryGetNode(path, out MemoryNode fileNode) == false)
        {
            throw new FileNotFoundException($"File not found: '{path}'");
        }

        if (fileNode.EntryType != VfsEntryType.File)
        {
            throw new IOException($"Path is not a file: '{path}'");
        }

        if (fileNode.Parent is MemoryDirectoryNode parentDir)
        {
            parentDir.RemoveChild(fileNode);
        }
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath) => MoveFile(sourcePath, destinationPath, true);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
        }

        if (sourceNode.EntryType != VfsEntryType.File)
        {
            throw new IOException($"Source path points to a directory: '{sourcePath}'");
        }

        bool destinationExists = TryGetNode(destinationPath, out MemoryNode destinationNode);
        if (destinationExists)
        {
            if (destinationNode.EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Destination path is a directory: '{destinationPath}'");
            }

            if (overwrite)
            {
                if (destinationNode.Parent is MemoryDirectoryNode destParent)
                {
                    destParent.RemoveChild(destinationNode);
                }
            }
            else
            {
                throw new IOException("File already exists");
            }
        }

        if (TryGetNode(destinationPath.GetDirectory(), out MemoryNode destFolderNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination directory not found: '{destinationPath}'");
        }

        if (destFolderNode.EntryType != VfsEntryType.Directory)
        {
            throw new IOException($"Destination parent is not a directory: '{destinationPath.GetDirectory()}'");
        }

        if (sourceNode.Parent == null)
        {
            throw new DetachedNodeException(sourceNode);
        }

        MemoryFileNode file = (MemoryFileNode)sourceNode;
        if (file.Parent is MemoryDirectoryNode prevParent)
        {
            prevParent.RemoveChild(file);
        }
        file.FullPath = destinationPath;
        ((MemoryDirectoryNode)destFolderNode).AddChild(file);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath) => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
        }

        if (sourceNode.EntryType != VfsEntryType.File)
        {
            throw new IOException($"Source path is not a file: '{sourcePath}'");
        }

        bool destinationExists = TryGetNode(destinationPath, out MemoryNode destinationNode);
        if (destinationExists)
        {
            if (destinationNode.EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Cannot overwrite directory with file: '{destinationPath}'");
            }

            if (overwrite)
            {
                if (destinationNode.Parent is MemoryDirectoryNode dParent)
                {
                    dParent.RemoveChild(destinationNode);
                }
            }
            else
            {
                throw new IOException("File already exists");
            }
        }

        if (TryGetNode(destinationPath.GetDirectory(), out MemoryNode destFolderNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination directory not found: '{destinationPath}'");
        }

        if (destFolderNode.EntryType != VfsEntryType.Directory)
        {
            throw new IOException($"Destination parent is not a directory: '{destinationPath.GetDirectory()}'");
        }

        MemoryFileNode clone = ((MemoryFileNode)sourceNode).Clone();
        clone.FullPath = destinationPath;
        ((MemoryDirectoryNode)destFolderNode).AddChild(clone);
    }

    // Filestreams
    public Stream OpenRead(VPath path)
    {
        if (TryGetNode(path, out MemoryNode node) == false)
        {
            throw new FileNotFoundException($"Path not found within backend: '{path}'");
        }

        if (node.EntryType == VfsEntryType.Directory)
        {
            throw new IOException($"Cannot open a directory as a file: '{path}'");
        }

        return ((MemoryFileNode)node).OpenRead();
    }

    public Stream OpenWrite(VPath path) => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        bool nodeFound = TryGetNode(path, out MemoryNode node);

        if (mode is FileMode.Open or FileMode.Append or FileMode.Truncate)
        {
            if (nodeFound == false)
            {
                throw new FileNotFoundException($"File not found: '{path}'");
            }
        }
        else
        {
            if (nodeFound == false)
            {
                CreateDirectory(path.GetDirectory());
                CreateFile(path);
                TryGetNode(path, out node);
            }
        }

        if (node.EntryType != VfsEntryType.File)
        {
            throw new IOException($"Path is not a file: '{path}'");
        }

        return node.OpenWrite(mode, access, share);
    }

    // Directory Operations
    public void CreateDirectory(VPath path)
    {
        if (Exists(path))
        {
            return;
        }

        string[] segments = path.Split();
        if (segments.Length == 0)
        {
            return;
        }

        VPath currentPath = VPath.Root;
        MemoryDirectoryNode parent = root;

        for (int i = 0; i < segments.Length; i++)
        {
            currentPath = currentPath.Append(segments[i]);

            if (TryGetNode(currentPath, out MemoryNode node))
            {
                parent = (MemoryDirectoryNode)node;
                continue;
            }

            MemoryDirectoryNode newNode = new(currentPath);
            parent.AddChild(newNode);
            parent = newNode;
        }
    }

    public void DeleteDirectory(VPath path) => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        if (TryGetNode(path, out MemoryNode node) == false)
        {
            throw new DirectoryNotFoundException($"Directory not found: '{path}'");
        }

        if (node.EntryType != VfsEntryType.Directory)
        {
            throw new DirectoryNotFoundException($"Path points to a file: '{path}'");
        }

        MemoryDirectoryNode dir = (MemoryDirectoryNode)node;

        if (recursive == false && dir.ChildCount > 0)
        {
            throw new IOException("The directory is not empty");
        }

        if (recursive)
        {
            foreach (MemoryDirectoryNode childDir in dir.Children.OfType<MemoryDirectoryNode>().ToList())
            {
                DeleteDirectory(childDir.FullPath, true);
            }

            foreach (MemoryFileNode file in dir.Children.OfType<MemoryFileNode>().ToList())
            {
                dir.RemoveChild(file);
            }
        }
        else
        {
            if (dir.ChildCount > 0)
            {
                throw new IOException("The directory is not empty");
            }
        }

        if (dir.Parent is MemoryDirectoryNode parentDir)
        {
            parentDir.RemoveChild(dir);
        }
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
        }
        if (sourceNode is not MemoryDirectoryNode sourceDir)
        {
            throw new IOException($"Source path is not a directory: '{sourcePath}'");
        }

        VPath destParentPath = destinationPath.GetDirectory();
        if (TryGetNode(destParentPath, out MemoryNode destParentNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination parent directory not found: '{destParentPath}'");
        }
        if (destParentNode is not MemoryDirectoryNode destParentDir)
        {
            throw new IOException($"Destination parent is not a directory: '{destParentPath}'");
        }

        if (TryGetNode(destinationPath, out _))
        {
            throw new IOException($"Destination directory already exists: '{destinationPath}'");
        }

        VPath ancestor = destParentPath;
        while (ancestor.IsRoot == false)
        {
            if (ancestor == sourcePath)
            {
                throw new IOException($"Cannot move a directory into its subtree: '{destinationPath}'");
            }
            ancestor = ancestor.GetDirectory();
        }

        if (sourceDir.Parent is MemoryDirectoryNode prevParent)
        {
            prevParent.RemoveChild(sourceDir);
        }
        sourceDir.FullPath = destinationPath;
        UpdateChildPaths(sourceDir, sourcePath, destinationPath);
        destParentDir.AddChild(sourceDir);
    }

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
        }
        if (sourceNode is not MemoryDirectoryNode sourceDir)
        {
            throw new IOException($"Source path is not a directory: '{sourcePath}'");
        }

        VPath destParentPath = destinationPath.GetDirectory();
        if (TryGetNode(destParentPath, out MemoryNode destParentNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination parent directory not found: '{destParentPath}'");
        }
        if (destParentNode is not MemoryDirectoryNode destParentDir)
        {
            throw new IOException($"Destination parent is not a directory: '{destParentPath}'");
        }

        if (Exists(destinationPath))
        {
            throw new IOException($"Destination directory already exists: '{destinationPath}'");
        }

        MemoryDirectoryNode newDir = new(destinationPath);
        destParentDir.AddChild(newDir);

        foreach (MemoryNode child in sourceDir.Children)
        {
            VPath childDestPath = destinationPath.Append(child.FullPath.GetLeaf());

            if (child is MemoryFileNode fileNode)
            {
                MemoryFileNode newFile = fileNode.Clone();
                newFile.FullPath = childDestPath;
                newDir.AddChild(newFile);
            }
            else if (child is MemoryDirectoryNode)
            {
                CopyDirectory(child.FullPath, childDestPath);
            }
        }
    }

    private bool TryGetNode(VPath path, out MemoryNode node)
    {
        if (path == VPath.Root)
        {
            node = root;
            return true;
        }

        if (path.IsEmpty)
        {
            node = null!;
            return false;
        }

        return TryTraversePath(path.Split(), out node);
    }

    private bool TryTraversePath(string[] segments, out MemoryNode node)
    {
        MemoryDirectoryNode current = root;

        for (int i = 0; i < segments.Length; i++)
        {
            bool last = i == segments.Length - 1;

            if (last)
            {
                if (current.TryGetChild(segments[i], out MemoryNode found))
                {
                    node = found;
                    return true;
                }
                node = null!;
                return false;
            }

            if (current.TryGetChild(segments[i], out MemoryNode next) == false || next is not MemoryDirectoryNode nextDir)
            {
                node = null!;
                return false;
            }
            current = nextDir;
        }

        node = null!;
        return false;
    }

    private void UpdateChildPaths(MemoryDirectoryNode directory, VPath oldBasePath, VPath newBasePath)
    {
        foreach (MemoryNode child in directory.Children)
        {
            string oldBase = oldBasePath.ToString();
            string childFull = child.FullPath.ToString();
            if (childFull.StartsWith(oldBase, StringComparison.Ordinal))
            {
                string relative = childFull[oldBase.Length..].TrimStart('/');
                if (relative.Length == 0)
                {
                    child.FullPath = newBasePath;
                }
                else
                {
                    child.FullPath = newBasePath.Append(relative);
                }
            }

            if (child is MemoryDirectoryNode childDir)
            {
                UpdateChildPaths(childDir, oldBasePath, newBasePath);
            }
        }
    }

    public void Dump(string physicalPath)
    {
        Directory.CreateDirectory(physicalPath);
        DumpNode(root, physicalPath);
    }

    private void DumpNode(MemoryNode node, string physicalBasePath)
    {
        if (node is MemoryDirectoryNode dirNode)
        {
            foreach (MemoryNode child in dirNode.Children)
            {
                string childPhysicalPath = Path.Combine(physicalBasePath, child.FullPath.GetLeaf());

                if (child is MemoryDirectoryNode)
                {
                    Directory.CreateDirectory(childPhysicalPath);
                    DumpNode(child, childPhysicalPath);
                }
                else if (child is MemoryFileNode fileNode)
                {
                    using Stream sourceStream = fileNode.OpenRead();
                    using FileStream destStream = File.Create(childPhysicalPath);
                    sourceStream.CopyTo(destStream);
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
                root.Dispose();
            }
            disposed = true;
        }
    }
}
