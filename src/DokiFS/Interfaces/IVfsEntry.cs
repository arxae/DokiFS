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

    Stream OpenRead(VPath path);
    Stream OpenWrite(VPath path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite);
}

public enum VfsEntryType
{
    File,
    Directory
}

[Flags]
public enum VfsEntryProperties
{
    Default,
    Readonly,
    Hidden
}
