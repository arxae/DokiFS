using System.Runtime.InteropServices;
using DokiFS.Backends.Physical.Exceptions;
using DokiFS.Interfaces;
using DokiFS.Internal;

namespace DokiFS.Backends.Physical;

public class PhysicalFileSystemBackend : IFileSystemBackend, IPhysicalPathProvider
{
    public BackendProperties BackendProperties => BackendProperties.PhysicalPaths;

    public string BackendRoot { get; private set; }

    public string RootPhysicalPath => BackendRoot;

    const string FileDescriptor = "Physical File";
    const string DirectoryDescriptor = "Physical Directory";

    readonly StringComparison pathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public PhysicalFileSystemBackend(string physicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(physicalPath);

        string fullRoot;
        try
        {
            physicalPath = VPath.ExpandSpecialFolders(physicalPath);
            fullRoot = Path.GetFullPath(physicalPath);
        }
        catch (Exception ex)
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' is invalid or inaccessible. See inner exception for details.", ex);
        }

        if (File.Exists(fullRoot))
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' points to a file instead of a folder");
        }

        if (Directory.Exists(fullRoot) == false)
        {
            throw new InvalidPathException($"The provided path '{physicalPath}' does not exist");
        }

        BackendRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
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
            return VfsEntry.FromFileSystemInfo(fileInfo, path, typeof(PhysicalFileSystemBackend), FileDescriptor);
        }
        else if (Directory.Exists(physicalPath))
        {
            DirectoryInfo dirInfo = new(physicalPath);
            return VfsEntry.FromFileSystemInfo(dirInfo, path, typeof(PhysicalFileSystemBackend), DirectoryDescriptor);
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
            .Select(d => VfsEntry.FromFileSystemInfo(d, path.Append(d.Name), typeof(PhysicalFileSystemBackend), DirectoryDescriptor));

        IEnumerable<VfsEntry> files = directoryInfo.EnumerateFiles()
            .Select(f => VfsEntry.FromFileSystemInfo(f, path.Append(f.Name), typeof(PhysicalFileSystemBackend), FileDescriptor));

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

        if (Directory.Exists(source))
        {
            throw new IOException($"Source path points to a directory, cannot move as file: '{sourcePath}'");
        }

        if (Directory.Exists(destination))
        {
            throw new IOException($"Destination path points to a directory, cannot move file to: '{destinationPath}'");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Move(source, destination, overwrite);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        TryGetPhysicalPath(sourcePath, out string source);
        string destination = GetPhysicalPath(destinationPath);

        if (Directory.Exists(source))
        {
            throw new IOException($"Source path points to a directory, cannot move as file: '{sourcePath}'");
        }

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
            throw new IOException($"Path points to a directory or is not readable: '{path}'");
        }
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        string physicalPath;

        if (mode is FileMode.Open or FileMode.Append or FileMode.Truncate)
        {
            if (TryGetPhysicalPath(path, out physicalPath) == false)
            {
                throw new FileNotFoundException($"File not found: '{path}'");
            }
        }
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
        string physicalPath = GetPhysicalPath(path);

        if (File.Exists(physicalPath))
        {
            throw new IOException($"Path points to a file: '{path}'");
        }

        if (Directory.Exists(physicalPath) == false)
        {
            throw new DirectoryNotFoundException($"Directory not found: '{path}'");
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

        if (Directory.Exists(source) == false)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        if (destination.StartsWith(source, pathComparison))
        {
            throw new IOException("Destination directory is within the source directory hierarchy.");
        }

        Directory.CreateDirectory(destination);

        foreach (string entry in Directory.EnumerateFileSystemEntries(source))
        {
            string name = Path.GetFileName(entry);
            string destChildPhysical = Path.Combine(destination, name);

            if (Directory.Exists(entry))
            {
                CopyDirectory(sourcePath.Append(name), destinationPath.Append(name));
            }
            else
            {
                File.Copy(entry, destChildPhysical, overwrite: true);
            }
        }
    }

    public bool TryGetPhysicalPath(VPath path, out string physicalPath)
    {
        try
        {
            string candidate = GetPhysicalPath(path); // normalized + boundary
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                physicalPath = candidate;
                return true;
            }
            physicalPath = null;
            return false;
        }
        catch
        {
            physicalPath = null;
            return false;
        }
    }

    string GetPhysicalPath(VPath path)
    {
        if (path.IsAbsolute)
        {
            path = path.FullPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        string combined = Path.Combine(BackendRoot, (string)path);
        string full = Path.GetFullPath(combined);

        if (full.StartsWith(BackendRoot, pathComparison) == false)
        {
            throw new UnauthorizedAccessException($"Resolved path '{full}' is outside the backend root '{BackendRoot}'.");
        }

        return full;
    }
}
