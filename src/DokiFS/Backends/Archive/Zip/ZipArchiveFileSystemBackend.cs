using System.IO.Compression;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Archive.Zip;

public class ZipArchiveFileSystemBackend : IFileSystemBackend, ICommit
{
    public BackendProperties BackendProperties =>
        BackendProperties.RequiresCommit
        | BackendProperties.Cached;

    public bool AutoCommit { get; set; }

    readonly string archivePath;
    readonly ZipArchiveMode zipMode;
    ZipArchive archive;

    const string NotSupportedExceptionMessage = "This operation is not supported in read-only mode.";

    public ZipArchiveFileSystemBackend(string archivePath)
        : this(archivePath, ZipArchiveMode.Read, false) { }

    public ZipArchiveFileSystemBackend(string archivePath, ZipArchiveMode mode, bool autoCommit)
    {
        this.archivePath = archivePath;
        zipMode = mode;
        AutoCommit = autoCommit;

        if (File.Exists(archivePath) == false)
        {
            throw new FileNotFoundException("File not found", archivePath);
        }

        archive = ZipFile.Open(archivePath, mode);
    }

    public UnmountResult OnUnmount() => UnmountResult.Accepted;
    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;

    public bool Exists(VPath path) => GetEntry(path) != null;

    public IVfsEntry GetInfo(VPath path)
    {
        ZipArchiveEntry entry = GetEntry(path)
            ?? throw new FileNotFoundException($"File not found in archive: {path}");

        VfsEntryType entryType = entry.FullName.EndsWith('/')
            ? VfsEntryType.Directory
            : VfsEntryType.File;
        bool isHidden = entry.FullName.StartsWith('.');

        return new ArchiveEntry(
            path,
            entryType,
            isHidden ? VfsEntryProperties.Hidden : VfsEntryProperties.None
        )
        {
            Size = entry.Length,
            CompressedSize = entry.CompressedLength,
            LastWriteTime = entry.LastWriteTime.UtcDateTime,
            FromBackend = GetType(),
            Description = entryType == VfsEntryType.Directory ? "ZIP Folder" : "Zip File"
        };
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        string dirPrefix = GetDirectoryPrefix(path);

        Dictionary<string, IVfsEntry> children = new(StringComparer.OrdinalIgnoreCase);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // For root we accept every entry; otherwise filter by prefix
            if (entry.FullName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase) == false && dirPrefix.Length > 0)
            {
                continue;
            }

            string remainder = entry.FullName[dirPrefix.Length..];

            // Don't include the directory itself
            if (remainder.Length == 0)
            {
                continue;
            }

            int slashIndex = remainder.IndexOf('/');
            // Is a directory
            if (slashIndex >= 0)
            {
                string childDirSegment = remainder[..(slashIndex + 1)];
                string childDirFull = VPath.DirectorySeparator + dirPrefix + childDirSegment;

                if (children.ContainsKey(childDirFull)) continue;

                VPath childDirPath = new(childDirFull);
                children[childDirFull] = new ArchiveEntry(childDirPath, VfsEntryType.Directory)
                {
                    Size = entry.Length,
                    CompressedSize = entry.CompressedLength,
                    LastWriteTime = entry.LastWriteTime.UtcDateTime,
                    FromBackend = GetType(),
                    Description = "ZIP Folder"
                };
            }
            else // Is a file
            {
                string childFileFull = VPath.DirectorySeparator + dirPrefix + remainder;
                if (children.ContainsKey(childFileFull) == false)
                {
                    children[childFileFull] = GetInfo(new VPath(childFileFull));
                }
            }
        }

        return children.Values;
    }

    static string GetDirectoryPrefix(VPath path)
    {
        string dirPrefix = path.FullPath.TrimStart('/');

        if (dirPrefix.Length > 0 && dirPrefix.EndsWith('/') == false)
        {
            dirPrefix += '/';
        }

        return dirPrefix;
    }

    public void CreateFile(VPath path, long size = 0)
    {
        if (zipMode == ZipArchiveMode.Read)
        {
            throw new NotSupportedException(NotSupportedExceptionMessage);
        }

        if (Exists(path))
        {
            throw new IOException($"File already exists: {path}");
        }

        ZipArchiveEntry entry = archive.CreateEntry(path.FullPath);

        if (size > 0)
        {
            using Stream stream = entry.Open();

            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];

            long remaining = size;
            while (remaining > 0)
            {
                int toWrite = (int)Math.Min(bufferSize, remaining);
                stream.Write(buffer, 0, toWrite);
                remaining -= toWrite;
            }

            stream.Flush();
        }

        if (AutoCommit) Commit();
    }

    public void DeleteFile(VPath path)
    {
        if (zipMode == ZipArchiveMode.Read)
        {
            throw new NotSupportedException(NotSupportedExceptionMessage);
        }

        if (Exists(path) == false)
        {
            throw new FileNotFoundException($"File does not exists: {path}");
        }

        ZipArchiveEntry entry = archive.Entries
            .FirstOrDefault(e => e.FullName.Equals(path.FullPath[1..], StringComparison.OrdinalIgnoreCase));
        entry?.Delete();

        if (AutoCommit) Commit();
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, true);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (zipMode == ZipArchiveMode.Read)
        {
            throw new NotSupportedException(NotSupportedExceptionMessage);
        }

        if (Exists(sourcePath) == false)
        {
            throw new IOException($"File does not exists: {sourcePath}");
        }

        if (Exists(destinationPath))
        {
            if (overwrite)
            {
                DeleteFile(destinationPath);
            }
            else
            {
                throw new FileNotFoundException($"Destination file already exists: {destinationPath}");
            }
        }

        ZipArchiveEntry sourceEntry = GetEntry(sourcePath);
        ZipArchiveEntry destinationEntry = archive.CreateEntry(destinationPath.FullPath);

        using (Stream sourceStream = sourceEntry.Open())
        using (Stream destinationStream = destinationEntry.Open())
        {
            sourceStream.CopyTo(destinationStream);
        }

        sourceEntry.Delete();

        if (AutoCommit) Commit();
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (zipMode == ZipArchiveMode.Read)
            throw new NotSupportedException(NotSupportedExceptionMessage);

        if (Exists(sourcePath) == false)
            throw new FileNotFoundException($"File does not exists: {sourcePath}");

        if (Exists(destinationPath))
        {
            if (overwrite)
            {
                DeleteFile(destinationPath);
            }
            else
            {
                throw new IOException($"Destination file already exists: {destinationPath}");
            }
        }

        ZipArchiveEntry sourceEntry = GetEntry(sourcePath);
        ZipArchiveEntry destinationEntry = archive.CreateEntry(destinationPath.FullPath);

        using (Stream sourceStream = sourceEntry.Open())
        using (Stream destinationStream = destinationEntry.Open())
        {
            sourceStream.CopyTo(destinationStream);
        }

        if (AutoCommit) Commit();
    }

    public Stream OpenRead(VPath path)
    {
        if (Exists(path) == false)
            throw new FileNotFoundException($"File does not exists: {path}");

        return new ReadOnlyStream(GetEntry(path).Open());
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        if (zipMode == ZipArchiveMode.Read)
            throw new NotSupportedException(NotSupportedExceptionMessage);

        bool exists = Exists(path);

        switch (mode)
        {
            case FileMode.CreateNew:
                if (exists) throw new IOException($"File already exists: {path}");
                break;
            case FileMode.Create:
                if (exists) DeleteFile(path);
                break;
            case FileMode.Open:
                if (exists == false) throw new FileNotFoundException($"File does not exist: {path}");
                break;
            case FileMode.Truncate:
                if (exists == false) throw new FileNotFoundException($"File does not exist: {path}");
                DeleteFile(path);
                break;
            case FileMode.Append:
                // ZIP entries do not support true append; emulate by extracting, buffering, rewriting.
                if (exists == false) throw new FileNotFoundException($"File does not exist: {path}");
                return OpenAppendEmulated(path);
            case FileMode.OpenOrCreate:
            default:
                break;
        }

        ZipArchiveEntry entry = GetEntry(path) ?? archive.CreateEntry(path.FullPath);
        return entry.Open();
    }

    Stream OpenAppendEmulated(VPath path)
    {
        ZipArchiveEntry existing = GetEntry(path);
        using MemoryStream buffer = new();
        using (Stream rs = existing.Open()) rs.CopyTo(buffer);
        byte[] existingBytes = buffer.ToArray();
        existing.Delete();
        ZipArchiveEntry recreated = archive.CreateEntry(path.FullPath);
        Stream ws = recreated.Open();
        ws.Write(existingBytes, 0, existingBytes.Length);

        return ws;
    }

    public void CreateDirectory(VPath path)
    {
        if (zipMode == ZipArchiveMode.Read)
            throw new NotSupportedException(NotSupportedExceptionMessage);

        ZipArchiveEntry entry = GetEntry(path);

        if (entry == null)
        {
            // A zip entry that ends with a / is considered a directory
            // For convenience, ensure the path ends with a directory separator, even when not provided
            if (path.IsDirectory == false)
            {
                path = path.Append(VPath.DirectorySeparatorString);
            }

            archive.CreateEntry(path.FullPath);
        }
        else
        {
            if (GetInfo(path).EntryType == VfsEntryType.File)
            {
                throw new IOException("A file with the same name already exists at the specified path.");
            }
        }

        if (AutoCommit) Commit();
    }

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        if (zipMode == ZipArchiveMode.Read)
            throw new NotSupportedException(NotSupportedExceptionMessage);

        if (path.IsDirectory == false)
        {
            path = path.Append(VPath.DirectorySeparatorString);
        }

        ZipArchiveEntry entry = GetEntry(path);

        // If entry does not exist, there might not be a dedicated entry for the directory,
        // instead, delete all the entries that start with the given path.

        if (entry != null)
        {
            ArchiveEntry info = GetInfo(path) as ArchiveEntry;

            if (info.EntryType == VfsEntryType.File)
            {
                throw new IOException("This path points to a file, not a directory.");
            }

            entry.Delete();
        }
        else
        {
            foreach (ZipArchiveEntry e in archive.Entries
                .Where(e => e.FullName.StartsWith(path.FullPath, StringComparison.OrdinalIgnoreCase)))
            {
                e.Delete();
            }
        }

        if (AutoCommit) Commit();
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (zipMode == ZipArchiveMode.Read)
        {
            throw new NotSupportedException(NotSupportedExceptionMessage);
        }

        if (Exists(sourcePath) == false)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");
        }

        if (Exists(destinationPath))
        {
            throw new IOException($"Destination directory already exists: {destinationPath}");
        }

        // Ensure paths are treated as directories
        if (sourcePath.IsDirectory == false)
        {
            sourcePath = sourcePath.Append(VPath.DirectorySeparatorString);
        }

        if (destinationPath.IsDirectory == false)
        {
            destinationPath = destinationPath.Append(VPath.DirectorySeparatorString);
        }

        // Get all entries that start with the source path
        List<ZipArchiveEntry> entriesToMove = [.. archive.Entries.Where(e => e.FullName.StartsWith(sourcePath.FullPath, StringComparison.OrdinalIgnoreCase))];

        // Create new entries at destination and copy content
        foreach (ZipArchiveEntry sourceEntry in entriesToMove)
        {
            string relativePath = sourceEntry.FullName[sourcePath.FullPath.Length..];
            string newPath = destinationPath.FullPath + relativePath;

            ZipArchiveEntry destinationEntry = archive.CreateEntry(newPath);

            if (sourceEntry.Length > 0) // Only copy content if it's not an empty directory entry
            {
                using Stream sourceStream = sourceEntry.Open();
                using Stream destinationStream = destinationEntry.Open();
                sourceStream.CopyTo(destinationStream);
            }
        }

        // Delete original entries
        foreach (ZipArchiveEntry entry in entriesToMove)
        {
            entry.Delete();
        }

        if (AutoCommit) Commit();
    }

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (zipMode == ZipArchiveMode.Read)
        {
            throw new NotSupportedException(NotSupportedExceptionMessage);
        }

        if (Exists(sourcePath) == false)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");
        }

        if (Exists(destinationPath))
        {
            throw new IOException($"Destination directory already exists: {destinationPath}");
        }

        // Ensure paths are treated as directories
        if (sourcePath.IsDirectory == false)
        {
            sourcePath = sourcePath.Append(VPath.DirectorySeparatorString);
        }
        if (destinationPath.IsDirectory == false)
        {
            destinationPath = destinationPath.Append(VPath.DirectorySeparatorString);
        }

        // Get all entries that start with the source path
        List<ZipArchiveEntry> entriesToCopy = [..
            archive.Entries.Where(e => e.FullName.StartsWith(sourcePath.FullPath, StringComparison.OrdinalIgnoreCase))];

        // Create new entries at destination and copy content
        foreach (ZipArchiveEntry sourceEntry in entriesToCopy)
        {
            string relativePath = sourceEntry.FullName[sourcePath.FullPath.Length..];
            string newPath = destinationPath.FullPath + relativePath;

            ZipArchiveEntry destinationEntry = archive.CreateEntry(newPath);

            if (sourceEntry.Length > 0) // Only copy content if it's not an empty directory entry
            {
                using Stream sourceStream = sourceEntry.Open();
                using Stream destinationStream = destinationEntry.Open();
                sourceStream.CopyTo(destinationStream);
            }
        }

        if (AutoCommit) Commit();
    }

    public void Commit()
    {
        if (zipMode == ZipArchiveMode.Read) return;

        archive.Dispose();
        archive = ZipFile.Open(archivePath, zipMode);
    }

    public void Discard() { }

    ZipArchiveEntry GetEntry(VPath path)
    {
        string target = path.FullPath.TrimStart('/'); // external "/x" to internal "x"
        return archive.Entries.FirstOrDefault(e => string.Equals(e.FullName.TrimStart('/'), target, StringComparison.OrdinalIgnoreCase));
    }
}
