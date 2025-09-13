using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendDeleteDirectoryTests : IDisposable
{
    readonly IoTestUtilities util;

    public PhysicalFileSystemBackendDeleteDirectoryTests()
    {
        util = new(nameof(PhysicalFileSystemBackendDeleteDirectoryTests));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "DeleteDirectory: Deletes directory")]
    public void ShouldDeleteDirectory()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string dir = "test";
        VPath path = $"/{dir}";

        util.CreateTempDirectory(dir);

        backend.DeleteDirectory(path);

        Assert.False(util.DirExists(dir));
    }

    [Fact(DisplayName = "DeleteDirectory: Throws exception on path to file")]
    public void ShouldThrowExceptionOnFilePath()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string dir = "test";
        VPath path = $"/{dir}";

        util.CreateTempFile(dir);

        Assert.Throws<IOException>(() => backend.DeleteDirectory(path));
    }
}
