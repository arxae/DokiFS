namespace DokiFS.Interfaces;

public interface IVfsEntry
{
    string FileName { get; }
    VPath FullPath { get; }
    VfsEntryType EntryType { get; }
    VfsEntryProperties Properties { get; }
    long Size { get; }
    DateTime LastWriteTime { get; }
    Type FromBackend { get; }
    string Description { get; }

    Stream OpenRead();
    Stream OpenWrite(FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read);
}

public enum VfsEntryType
{
    File,
    Directory,
    MountPoint,
    Virtual,
    Archive
}

[Flags]
public enum VfsEntryProperties
{
    None = 0,
    Readonly = 1,
    Hidden = 2,
    Virtual = 4
}
