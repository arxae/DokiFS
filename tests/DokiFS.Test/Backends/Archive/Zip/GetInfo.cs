using DokiFS.Backends.Archive;
using DokiFS.Backends.Archive.Zip;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendGetInfoTests : IDisposable
{
    readonly IoTestUtilities util;
    readonly string archivePath;
    readonly ZipArchiveFileSystemBackend backend;

    public ZipArchiveBackendGetInfoTests()
    {
        util = new(nameof(ZipArchiveBackendExistsTests));

        archivePath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendExistsTests)}.zip");
        ResourceReader.WriteResourceToFile("DokiFS.Test.Backends.Archive.Zip.Assets.ZipArchiveTest.zip", archivePath);

        backend = new(archivePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "GetInfo: Basic get info for existing file")]
    public void GetInfoBasicGetInfoForExistingFile()
    {
        VPath path = "/toplevelfile.txt";
        IVfsEntry fileInfo = backend.GetInfo(path);

        Assert.NotNull(fileInfo);
        Assert.Equal(path.GetFileName(), fileInfo.FileName);
        Assert.Equal(path.FullPath, fileInfo.FullPath);
        Assert.Equal(VfsEntryType.File, fileInfo.EntryType);
        Assert.NotEqual(VfsEntryProperties.Readonly, fileInfo.Properties);
        Assert.NotEqual(VfsEntryProperties.Hidden, fileInfo.Properties);
        Assert.Equal(32, fileInfo.Size);
        Assert.True(fileInfo.LastWriteTime < DateTime.Now);
        Assert.Equal(typeof(ArchiveEntry), fileInfo.GetType());
        Assert.Equal(typeof(ZipArchiveFileSystemBackend), fileInfo.FromBackend);
        Assert.True(fileInfo is ArchiveEntry);
        Assert.True(((ArchiveEntry)fileInfo).CompressedSize >= 0);
        Assert.Equal("Zip File", fileInfo.Description);
    }

    [Fact(DisplayName = "GetInfo: Basic get info for existing directory")]
    public void GetInfoBasicGetInfoForExistingDirectory()
    {
        VPath path = "/topleveldir/";
        IVfsEntry fileInfo = backend.GetInfo(path);

        Assert.Equal(VfsEntryType.Directory, fileInfo.EntryType);
    }

    [Fact(DisplayName = "GetInfo: Get info for non-existent file returns null")]
    public void GetInfoNonExistentFileReturnsNull()
    {
        VPath path = "/nonexistentfile.txt";

        Assert.Throws<FileNotFoundException>(() => backend.GetInfo(path));
    }
}
