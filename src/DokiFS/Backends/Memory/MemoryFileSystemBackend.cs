using System.Data;
using DokiFS.Backends.Memory.Nodes;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Memory;

public class MemoryFileSystemBackend : IFileSystemBackend, IDisposable
{
    public BackendProperties BackendProperties => BackendProperties.Transient;

    readonly MemoryRoot root;

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
                return dirNode.Children.Cast<IVfsEntry>();
            }
            else
            {
                throw new InvalidOperationException($"Path '{path}' is not a directory.");
            }
        }

        throw new FileNotFoundException($"Path not found within backend: '{path}'");
    }

    // File Operations
    public void CreateFile(VPath path, long size = 0)
    {
        VPath parentDirectoryPath = path.GetDirectory();

        if (TryGetNode(parentDirectoryPath, out MemoryNode parentNode) == false)
        {
            CreateDirectory(parentDirectoryPath);
        }

        MemoryDirectoryNode parentDirectory = parentNode as MemoryDirectoryNode;

        if (TryGetNode(path, out MemoryNode _) == false)
        {
            MemoryFileNode fileNode = new(path.GetFileName())
            {
                LastWriteTime = DateTime.UtcNow
            };
            fileNode.SetSize(size);

            parentDirectory.AddChild(fileNode);
        }
    }

    public void DeleteFile(VPath path)
    {
        if (TryGetNode(path, out MemoryNode fileNode))
        {
            MemoryDirectoryNode parentDirectory = fileNode.Parent as MemoryDirectoryNode;
            parentDirectory?.RemoveChild(fileNode);
        }
        else
        {
            throw new FileNotFoundException($"File not found: '{path}'");
        }
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, true);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        // Check source
        bool sourcePathFound = TryGetNode(sourcePath, out MemoryNode sourceNode);

        if (sourcePathFound == false)
        {
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
        }

        if (sourceNode.EntryType == VfsEntryType.Directory)
        {
            throw new IOException($"Source path points to a directory, cannot move directory: '{sourcePath}'");
        }

        // Check destination
        // Check if file with new name already exists
        bool destinationAlreadyExists = TryGetNode(destinationPath, out MemoryNode destinationNode);
        if (destinationAlreadyExists)
        {
            if (destinationNode.EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Destination path points to a directory, cannot move file to: '{destinationPath}'");
            }

            if (overwrite)
            {
                (destinationNode.Parent as MemoryDirectoryNode).RemoveChild(destinationNode);
            }
            else
            {
                throw new IOException($"File already exists");
            }
        }

        bool destinationFolderFound = TryGetNode(sourcePath.GetDirectory(), out MemoryNode destinationFolderNode);
        if (destinationFolderFound == false)
        {
            throw new DirectoryNotFoundException($"Destination directory not found: '{destinationPath}'");
        }

        // Move node
        MemoryDirectoryNode parent = sourceNode.Parent as MemoryDirectoryNode;
        MemoryFileNode file = sourceNode as MemoryFileNode;

        // Remove from the original folder
        parent.RemoveChild(file);

        // Rename the file
        file.FullPath = destinationPath;
        (destinationFolderNode as MemoryDirectoryNode).AddChild(file);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        // Check source
        bool sourcePathFound = TryGetNode(sourcePath, out MemoryNode sourceNode);

        if (sourcePathFound == false)
        {
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
        }

        // Check destination
        // Check if file with new name already exists
        bool destinationAlreadyExists = TryGetNode(destinationPath, out MemoryNode destinationNode);
        if (destinationAlreadyExists)
        {
            if (overwrite)
            {
                (destinationNode.Parent as MemoryDirectoryNode).RemoveChild(destinationNode);
            }
            else
            {
                throw new IOException($"File already exists");
            }
        }

        bool destinationFolderFound = TryGetNode(destinationPath.GetDirectory(), out MemoryNode destinationFolderNode);
        if (destinationFolderFound == false)
        {
            throw new DirectoryNotFoundException($"Destination directory not found: '{destinationPath}'");
        }

        if (destinationFolderNode.EntryType == VfsEntryType.File)
        {
            throw new IOException($"Destination path points to a directory, cannot move file to: '{destinationPath}'");
        }

        // Create a copy of the node
        MemoryFileNode file = sourceNode as MemoryFileNode;

        MemoryFileNode cloneNode = file.Clone();
        // Rename the file
        cloneNode.FullPath = destinationPath;

        (destinationFolderNode as MemoryDirectoryNode).AddChild(cloneNode);
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

        MemoryFileNode file = node as MemoryFileNode;

        return file.OpenRead();
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

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

        return node.OpenWrite(mode, access, share);
    }

    // Directory Operations
    public void CreateDirectory(VPath path)
    {
        // Path already exists, just return
        if (Exists(path)) return;

        string[] segments = path.Split();

        // This is the backend root
        if (segments.Length == 0) return;

        // Start building from the root
        VPath currentPath = VPath.Root;

        MemoryDirectoryNode parent = root;
        for (int i = 0; i < segments.Length; i++)
        {
            currentPath = currentPath.Combine(segments[i]);

            // Try to get next node
            if (TryGetNode(currentPath, out MemoryNode node))
            {
                parent = node as MemoryDirectoryNode;
                continue;
            }
            // Node doesn't exist, create it
            else
            {
                MemoryDirectoryNode newNode = new(currentPath);
                parent.AddChild(newNode);
                parent = newNode;
            }
        }
    }

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        bool foundNode = TryGetNode(path, out MemoryNode node);

        if (foundNode == false) throw new DirectoryNotFoundException($"Directory not found: '{path}'");
        if (node.EntryType == VfsEntryType.File)
        {
            throw new DirectoryNotFoundException($"Path points to a file, not a directory: '{path}'");
        }

        MemoryDirectoryNode dirNode = node as MemoryDirectoryNode;

        // Remove all the children first
        if (recursive == false && dirNode.Children.Count > 0)
        {
            throw new IOException("The directory is not empty");
        }

        if (dirNode.HasDirectoryChildren() && recursive)
        {
            List<MemoryNode> children = [.. dirNode.Children];
            foreach (MemoryNode childNode in children)
            {
                DeleteDirectory(childNode.FullPath, recursive);
            }
        }

        // detach the children
        dirNode.Children
            .Where(c => c is MemoryFileNode)
            .ToList()
            .ForEach(dirNode.RemoveChild);
        // detach the directory from its parent
        (dirNode.Parent as MemoryDirectoryNode)?.RemoveChild(dirNode);
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        // Check source exists
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
        }

        if (sourceNode is not MemoryDirectoryNode sourceDir)
        {
            throw new IOException($"Source path is not a directory: '{sourcePath}'");
        }

        // Check destination parent exists
        VPath destParentPath = destinationPath.GetDirectory();
        if (TryGetNode(destParentPath, out MemoryNode destParentNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination parent directory not found: '{destParentPath}'");
        }

        if (destParentNode is not MemoryDirectoryNode destParentDir)
        {
            throw new IOException($"Destination parent is not a directory: '{destParentPath}'");
        }

        // Check if destination already exists
        if (TryGetNode(destinationPath, out MemoryNode existingDestNode))
        {
            throw new IOException($"Destination directory already exists: '{destinationPath}'");
        }

        // Check if destination is a subdirectory of source
        VPath testPath = destParentPath;
        while (testPath.IsRoot == false)
        {
            if (testPath == sourcePath)
            {
                throw new IOException($"Cannot move a directory to one of its subdirectories: '{destinationPath}'");
            }
            testPath = testPath.GetDirectory();
        }

        // Remove from source parent
        MemoryDirectoryNode sourceParent = sourceDir.Parent as MemoryDirectoryNode;
        sourceParent.RemoveChild(sourceDir);

        // Update path and add to destination parent
        sourceDir.FullPath = destinationPath;
        UpdateChildPaths(sourceDir, sourcePath, destinationPath);
        destParentDir.AddChild(sourceDir);
    }

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        // Check source exists
        if (TryGetNode(sourcePath, out MemoryNode sourceNode) == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
        }

        if (sourceNode is not MemoryDirectoryNode sourceDir)
        {
            throw new IOException($"Source path is not a directory: '{sourcePath}'");
        }

        // Check destination parent exists
        VPath destParentPath = destinationPath.GetDirectory();
        if (TryGetNode(destParentPath, out MemoryNode destParentNode) == false)
        {
            throw new DirectoryNotFoundException($"Destination parent directory not found: '{destParentPath}'");
        }

        if (destParentNode is not MemoryDirectoryNode destParentDir)
        {
            throw new IOException($"Destination parent is not a directory: '{destParentPath}'");
        }

        // Check if destination already exists
        if (Exists(destinationPath))
        {
            throw new IOException($"Destination directory already exists: '{destinationPath}'");
        }

        // Create new destination directory
        MemoryDirectoryNode newDir = new(destinationPath);
        destParentDir.AddChild(newDir);

        // Recursively copy all children
        foreach (MemoryNode child in sourceDir.Children)
        {
            VPath childDestPath = destinationPath.Combine(child.FullPath.GetFileName());

            if (child is MemoryFileNode fileNode)
            {
                // Copy file
                MemoryFileNode newFile = fileNode.Clone();
                newFile.FullPath = childDestPath;
                newDir.AddChild(newFile);
            }
            else if (child is MemoryDirectoryNode dirNode)
            {
                // Recursively copy directory
                CopyDirectory(child.FullPath, childDestPath);
            }
        }
    }

    bool TryGetNode(VPath path, out MemoryNode node)
    {
        // No lock here, assume caller holds the lock
        if (path == VPath.Root)
        {
            node = root;
            return true;
        }

        if (path.IsEmpty || path.IsNull)
        {
            node = null;
            return false;
        }

        string[] segments = path.Split();

        // start at root and traverse until child is found
        VPath currentPath = VPath.Root;
        MemoryDirectoryNode currentNode = root;

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];

            // If it's the last segment, we are looking for either a file or directory
            if (i == segments.Length - 1)
            {
                // First try to find a file with this name
                if (currentNode.Children.FirstOrDefault(c => c.FullPath.GetFileName() == segment) is MemoryFileNode fileNode)
                {
                    node = fileNode;
                    return true;
                }

                // Then try to find a directory with this name
                if (currentNode.Children.FirstOrDefault(c => c.FullPath.GetFileName() == segment && c is MemoryDirectoryNode) is MemoryDirectoryNode dirNode)
                {
                    node = dirNode;
                    return true;
                }

                // Neither found
                node = null;
                return false;
            }
            else
            {
                // Traverse directories
                if (currentNode.Children.FirstOrDefault(c => c.FullPath.GetFileName() == segment && c is MemoryDirectoryNode) is MemoryDirectoryNode nextDir)
                {
                    currentNode = nextDir;
                    currentPath = currentPath.Combine(segment);
                }
                else
                {
                    node = null;
                    return false; // Directory not found
                }
            }
        }

        node = null;
        return false;
    }

    void UpdateChildPaths(MemoryDirectoryNode directory, VPath oldBasePath, VPath newBasePath)
    {
        foreach (MemoryNode child in directory.Children)
        {
            // Calculate the relative path from the old base path
            string relativePath = child.FullPath.ToString()[oldBasePath.ToString().Length..];
            // Create the new path by combining the new base path with the relative path
            VPath newPath = newBasePath.Combine(relativePath.TrimStart('/'));
            child.FullPath = newPath;

            // Recursively update children of directories
            if (child is MemoryDirectoryNode childDir)
            {
                UpdateChildPaths(childDir, oldBasePath, newBasePath);
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        root.Dispose();
    }
}
