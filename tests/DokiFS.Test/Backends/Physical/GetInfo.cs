using DokiFS.Backends.Physical;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendGetInfoTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendGetInfoTests()
    {
        util = new("GetInfo");
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact(DisplayName = "GetInfo: Basic get info for existing file")]
    public void GetInfoBasicGetInfoForExistingFile()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string fileName = "file.txt";
        util.CreateTempFile(fileName);

        VPath path = $"/{fileName}";
        IVfsEntry fileInfo = backend.GetInfo(path);

        Assert.NotNull(fileInfo);
        Assert.Equal(fileName, fileInfo.FileName);
        Assert.Equal(path.FullPath, fileInfo.FullPath);
        Assert.Equal(VfsEntryType.File, fileInfo.EntryType);
        Assert.NotEqual(VfsEntryProperties.Readonly, fileInfo.Properties);
        Assert.NotEqual(VfsEntryProperties.Hidden, fileInfo.Properties);
        Assert.Equal(0, fileInfo.Size);
        Assert.True(fileInfo.LastWriteTime < DateTime.Now);
        Assert.Equal(typeof(PhysicalFileSystemBackend), fileInfo.FromBackend);
    }

    [Fact(DisplayName = "GetInfo: Get info for non-existent file returns null")]
    public void GetInfoNonExistentFileReturnsNull()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);
        string dirName = "folder";
        util.CreateTempDirectory(dirName);

        VPath path = $"/{dirName}";
        IVfsEntry fileInfo = backend.GetInfo(path);

        Assert.Equal(VfsEntryType.Directory, fileInfo.EntryType);
    }
}
