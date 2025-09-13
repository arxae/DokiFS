using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendDeleteFileTests : IDisposable
{
    readonly IoTestUtilities util;

    public PhysicalFileSystemBackendDeleteFileTests()
    {
        util = new(nameof(PhysicalFileSystemBackendDeleteFileTests));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "DeleteFile: Basic Delete File")]
    public void ShouldDeleteFile()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string file = "test.txt";
        string path = $"/{file}";

        util.CreateTempFile(file);

        Assert.True(util.FileExists(file));

        backend.DeleteFile(path);

        Assert.False(util.FileExists(file));
    }

    [Fact(DisplayName = "DeleteFile: Delete non-existing file")]
    public void DeleteNonExistingFileThrowsException()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string file = "test.txt";
        string path = $"/{file}";

        Assert.False(util.FileExists(file));

        _ = Assert.Throws<FileNotFoundException>(() => backend.DeleteFile(path));
    }

    [Fact(DisplayName = "DeleteFile: Delete folder throws exception")]
    public void TryingToDeleteDirectoryThrowsException()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string path = "/test";
        util.CreateTempDirectory("test");

        _ = Assert.Throws<IOException>(() => backend.DeleteFile(path));
    }
}
