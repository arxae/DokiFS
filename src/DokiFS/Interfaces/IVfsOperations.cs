namespace DokiFS.Interfaces;

public interface IVfsOperations
{
    // Queries
    bool Exists(VPath path);
    IVfsEntry GetInfo(VPath path);
    IEnumerable<IVfsEntry> ListDirectory(VPath path);

    // File Operations
    void CreateFile(VPath path, long size = 0);
    void DeleteFile(VPath path);
    void MoveFile(VPath sourcePath, VPath destinationPath);
    void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite);
    void CopyFile(VPath sourcePath, VPath destinationPath);
    void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite);

    // Filestreams
    Stream OpenRead(VPath path);
    Stream OpenWrite(VPath path);
    Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share);

    // Directory Operations
    void CreateDirectory(VPath path);
    void DeleteDirectory(VPath path);
    void DeleteDirectory(VPath path, bool recursive);
    void MoveDirectory(VPath sourcePath, VPath destinationPath);
    void CopyDirectory(VPath sourcePath, VPath destinationPath);
}
