using DokiFS.Backends.Archive.Zip;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendListDirectoryTests : IDisposable
{
    readonly IoTestUtilities util;
    readonly string archivePath;
    readonly ZipArchiveFileSystemBackend backend;

    public ZipArchiveBackendListDirectoryTests()
    {
        util = new(nameof(ZipArchiveBackendListDirectoryTests));

        archivePath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendListDirectoryTests)}.zip");
        ResourceReader.WriteResourceToFile("DokiFS.Test.Backends.Archive.Zip.Assets.ZipArchiveTest.zip", archivePath);

        backend = new(archivePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "ListDirectory: Basic listing")]
    public void ListDirectoriesBasicListing()
    {
        VPath file = "/toplevelfile.txt";
        VPath topLevelDir = "/topleveldir/";

        IEnumerable<IVfsEntry> rootInfo = backend.ListDirectory(VPath.Root);
        IEnumerable<IVfsEntry> subInfo = backend.ListDirectory(topLevelDir);

        Assert.Equal(3, rootInfo.Count());
        Assert.Equal(2, subInfo.Count());

        // Check folder
        Assert.Equal(topLevelDir, rootInfo.First().FullPath.GetDirectory());
        Assert.Equal($"/{topLevelDir}", rootInfo.First().FullPath);
        Assert.Equal(VfsEntryType.Directory, rootInfo.First().EntryType);
        Assert.Equal(typeof(ZipArchiveFileSystemBackend), rootInfo.First().FromBackend);

        // Check file
        IVfsEntry? actualFile = rootInfo.FirstOrDefault(e => e.FullPath == file);
        Assert.NotNull(actualFile);
        Assert.Equal(file, actualFile.FullPath);
        Assert.Equal(VfsEntryType.File, actualFile.EntryType);
        Assert.Equal(32, actualFile.Size);
    }
}
