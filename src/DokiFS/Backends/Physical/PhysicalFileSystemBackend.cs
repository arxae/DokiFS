using System.Runtime.InteropServices;
using DokiFS.Backends.Physical.Exceptions;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Physical;

public class PhysicalFileSystemBackend : IFileSystemBackend, IPhysicalPathProvider
{
    public BackendProperties BackendProperties => BackendProperties.PhysicalPaths;

    public string BackendRoot { get; private set; }

    public string RootPhysicalPath => BackendRoot;

    public PhysicalFileSystemBackend(string physicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(physicalPath);

        // If an absolute path is used, get the full path
        string fullRoot;
        try
        {
            fullRoot = Path.GetFullPath(physicalPath);
        }
        catch (Exception ex)
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' is invalid or inaccessible. See inner exception for details.", ex);
        }

        if (File.Exists(fullRoot))
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' Points to a file instead of a folder");
        }

        if (Directory.Exists(fullRoot) == false)
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' does not exist");
        }

        BackendRoot = physicalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    // --- File/Folder queries
    public bool Exists(VPath path)
    {
        TryGetPhysicalPath(path, out string physicalPath);

        return File.Exists(physicalPath) || Directory.Exists(physicalPath);
    }

    public IVfsEntry GetInfo(VPath path)
    {
        TryGetPhysicalPath(path, out string physicalPath);

        if (File.Exists(physicalPath))
        {
            FileInfo fileInfo = new(physicalPath);
            return VfsEntry.FromFileSystemInfo(fileInfo, path, typeof(PhysicalFileSystemBackend), "PhysicalFileSystemBackend:55");
        }
        else if (Directory.Exists(physicalPath))
        {
            DirectoryInfo dirInfo = new(physicalPath);
            return VfsEntry.FromFileSystemInfo(dirInfo, path, typeof(PhysicalFileSystemBackend), "PhysicalFileSystemBackend:60");
        }

        throw new FileNotFoundException($"Path not found within backend: '{path}'");
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        TryGetPhysicalPath(path, out string physicalPath);

        if (Directory.Exists(physicalPath) == false)
        {
            throw new DirectoryNotFoundException($"Directory not found: '{physicalPath}'");
        }

        if (File.Exists(physicalPath))
        {
            throw new IOException($"The path provided points to a file, not a directory: '{physicalPath}'");
        }

        DirectoryInfo directoryInfo = new(physicalPath);

        IEnumerable<VfsEntry> directories = directoryInfo.EnumerateDirectories()
            .Select(d => VfsEntry.FromFileSystemInfo(d, path.Append(d.Name), typeof(PhysicalFileSystemBackend), "Physical Folder"));

        IEnumerable<VfsEntry> files = directoryInfo.EnumerateFiles()
            .Select(f => VfsEntry.FromFileSystemInfo(f, path.Append(f.Name), typeof(PhysicalFileSystemBackend), "Physical File"));

        return directories.Concat(files);
    }

    // --- File operations
    public void CreateFile(VPath path, long size = 0)
    {
        string physicalPath = GetPhysicalPath(path);

        if (Directory.Exists(physicalPath))
        {
            throw new IOException($"Path points to a directory: '{path}'");
        }

        // Check if the directory tree exists, otherwise create it
        string parentDirectory = Path.GetDirectoryName(physicalPath);
        Directory.CreateDirectory(parentDirectory);

        using FileStream fs = File.Create(physicalPath);

        if (size > 0)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[Math.Min(bufferSize, size)];

            long remaining = size;
            while (remaining > 0)
            {
                int currentChunk = (int)Math.Min(buffer.Length, remaining);
                fs.Write(buffer, 0, currentChunk);
                remaining -= currentChunk;
            }
        }
    }

    public void DeleteFile(VPath path)
    {
        TryGetPhysicalPath(path, out string physicalPath);

        if (Directory.Exists(physicalPath))
        {
            throw new IOException($"Path points to a directory: '{path}'");
        }

        if (File.Exists(physicalPath) == false)
        {
            throw new FileNotFoundException($"File not found: '{path}'");
        }

        // On non-windows systems we need to use lsof to check if a file is in use, since deleting an in use
        // fails silently on these platforms
        // On windows, File.Delete throws an exception when the file is in use.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false && OSUtils.UnixFileInUse(physicalPath))
        {
            throw new IOException($"File '{path}' is in use by another process and cannot be deleted.");
        }

        File.Delete(physicalPath);
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, true);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (TryGetPhysicalPath(sourcePath, out string source) == false)
        {
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
        }

        string destination = GetPhysicalPath(destinationPath);

        // Check if source is a directory
        if (Directory.Exists(source))
        {
            throw new IOException($"Source path points to a directory, cannot move as file: '{sourcePath}'");
        }

        // Check if destination is a directory
        if (Directory.Exists(destination))
        {
            throw new IOException($"Destination path points to a directory, cannot move file to: '{destinationPath}'");
        }

        // Ensure file tree
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Move(source, destination, overwrite);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        TryGetPhysicalPath(sourcePath, out string source);
        string destination = GetPhysicalPath(destinationPath);

        // Check if source is a directory
        if (Directory.Exists(source))
        {
            throw new IOException($"Source path points to a directory, cannot move as file: '{sourcePath}'");
        }

        // Check if destination is a directory
        if (Directory.Exists(destination))
        {
            throw new IOException($"Destination path points to a directory, cannot move file to: '{destinationPath}'");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(source, destination, overwrite);
    }

    public Stream OpenRead(VPath path)
    {
        if (TryGetPhysicalPath(path, out string physicalPath) == false)
        {
            throw new FileNotFoundException($"File not found: '{path}'");
        }

        try
        {
            return File.OpenRead(physicalPath);
        }
        catch (UnauthorizedAccessException)
        {
            // Remap to a more accurate exception
            throw new IOException($"Path points to a directory: '{path}'");
        }
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        string physicalPath;

        // If it's an "open" mode, the file should exist
        if (mode is FileMode.Open or FileMode.Append or FileMode.Truncate)
        {
            if (TryGetPhysicalPath(path, out physicalPath) == false)
            {
                throw new FileNotFoundException($"File not found: '{path}'");
            }
        }
        // Otherwise, just get the path to it. Opening a filestream
        // will create it
        else
        {
            physicalPath = GetPhysicalPath(path);
        }

        string parentDirectory = Path.GetDirectoryName(physicalPath);

        Directory.CreateDirectory(parentDirectory);

        return new FileStream(physicalPath, mode, access, share);
    }

    // --- Directory operations
    public void CreateDirectory(VPath path)
    {
        string physicalPath = GetPhysicalPath(path);
        Directory.CreateDirectory(physicalPath);
    }

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        TryGetPhysicalPath(path, out string physicalPath);

        // On Mac, Directory.Delete throws a DirectoryNotFoundException instead of an IOException.
        // Check here to make sure the exceptions are the same across platforms
        if (GetInfo(path).EntryType == VfsEntryType.File)
        {
            throw new IOException($"Path points to a file: '{path}'");
        }

        Directory.Delete(physicalPath, recursive);
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        TryGetPhysicalPath(sourcePath, out string source);
        string destination = GetPhysicalPath(destinationPath);

        Directory.Move(source, destination);
    }

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        TryGetPhysicalPath(sourcePath, out string source);
        string destination = GetPhysicalPath(destinationPath);

        DirectoryInfo dir = new(source);

        // Check if the source directory exists
        if (dir.Exists == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }
        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destination);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destination, file.Name);
            file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    public bool TryGetPhysicalPath(VPath path, out string physicalPath)
    {
        // Trim the leading / from the virtual path
        if (path.IsAbsolute)
        {
            path = path.FullPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        try
        {
            physicalPath = Path.GetFullPath(Path.Combine(BackendRoot, (string)path));
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid character(s) in path", ex);
        }

        // Verify that the path is actually part of the backend.
        if (physicalPath.StartsWith(BackendRoot, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new UnauthorizedAccessException($"Resolved path '{physicalPath}' is outside the backend root '{BackendRoot}'.");
        }

        if (File.Exists(physicalPath) || Directory.Exists(physicalPath))
        {
            return true;
        }

        physicalPath = null;
        return false;
    }

    // Gets the physical path to a file, even if it doesn't exist
    string GetPhysicalPath(VPath path)
    {
        if (path.IsAbsolute)
        {
            path = path.FullPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.Combine(BackendRoot, (string)path);
    }
}
