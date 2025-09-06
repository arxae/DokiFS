namespace DokiFS.Backends.Journal;

public enum JournalOperations
{
    CreateFile,
    DeleteFile,
    MoveFile,
    CopyFile,
    OpenWrite,
    CreateDirectory,
    DeleteDirectory,
    MoveDirectory,
    CopyDirectory,

    StreamWrite,
    CloseWriteStream,
}
