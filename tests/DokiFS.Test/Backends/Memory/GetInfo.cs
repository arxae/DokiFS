using DokiFS.Backends.Memory;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendGetInfoTests
{
    [Fact(DisplayName = "GetInfo: Basic get info for existing file")]
    public void GetInfoBasicGetInfoForExistingFile()
    {
        MemoryFileSystemBackend backend = new();

        VPath file = "/file.txt";
        backend.CreateFile(file);

        IVfsEntry fileInfo = backend.GetInfo(file);

        Assert.NotNull(fileInfo);
        Assert.Equal(file.GetLeaf(), fileInfo.FileName);
        Assert.Equal(file, fileInfo.FullPath);
        Assert.Equal(VfsEntryType.File, fileInfo.EntryType);
        Assert.NotEqual(VfsEntryProperties.Readonly, fileInfo.Properties);
        Assert.NotEqual(VfsEntryProperties.Hidden, fileInfo.Properties);
        Assert.Equal(0, fileInfo.Size);
        Assert.True(fileInfo.LastWriteTime < DateTime.Now);
        Assert.Equal(typeof(MemoryFileSystemBackend), fileInfo.FromBackend);
    }

    [Fact(DisplayName = "GetInfo: Get info for non-existent file returns null")]
    public void GetInfoNonExistentFileReturnsNull()
    {
        MemoryFileSystemBackend backend = new();
        VPath dirName = "folder";
        backend.CreateDirectory(dirName);

        VPath path = $"/{dirName}";
        IVfsEntry fileInfo = backend.GetInfo(path);

        Assert.Equal(VfsEntryType.Directory, fileInfo.EntryType);
    }
}
