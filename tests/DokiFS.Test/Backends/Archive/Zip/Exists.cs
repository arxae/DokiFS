using DokiFS.Backends.Archive.Zip;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendExistsTests : IDisposable
{
    readonly IoTestUtilities util;
    readonly string archivePath;
    readonly ZipArchiveFileSystemBackend backend;

    public ZipArchiveBackendExistsTests()
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

    [Fact(DisplayName = "Exists: Finds file existing in archive")]
    public void FindsExistingFile()
    {
        VPath file = "/toplevelfile.txt";
        bool result = backend.Exists(file);

        Assert.True(result);
    }

    [Fact(DisplayName = "Exists: Finds folder existing in archive")]
    public void FindsExistingFolder()
    {
        VPath dir = "/topleveldir/";
        bool result = backend.Exists(dir);

        Assert.True(result);
    }

    [Fact(DisplayName = "Exists: Returns false exception on non-existing file")]
    public void NonExistingFile()
    {
        VPath file = "/nonexistingfile.txt";
        bool result = backend.Exists(file);

        Assert.False(result);
    }
}
