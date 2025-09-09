namespace DokiFS.Backends.Journal;

public enum JournalOperations
{
    // VFS Operations
    CreateFile,
    DeleteFile,
    MoveFile,
    CopyFile,
    OpenWrite,
    CreateDirectory,
    DeleteDirectory,
    MoveDirectory,
    CopyDirectory,

    // Backend specific operations
    StreamWrite,
    CloseWriteStream,
}
