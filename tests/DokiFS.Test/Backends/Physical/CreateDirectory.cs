using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendCreateDirectoryTests : IDisposable
{
    readonly IoTestUtilities util;

    public PhysicalFileSystemBackendCreateDirectoryTests()
    {
        util = new(nameof(PhysicalFileSystemBackendCreateDirectoryTests));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "CreateDirectory: Creates directory")]
    public void ShouldCreateDirectory()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string dir = "test";
        VPath path = $"/{dir}";

        backend.CreateDirectory(path);

        Assert.True(util.DirExists(dir));
    }

    [Fact(DisplayName = "CreateDirectory: Throws exception on path to file")]
    public void ShouldThrowExceptionOnFilePath()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string dir = "test";
        VPath path = $"/{dir}";

        util.CreateTempFile(dir);

        Assert.Throws<IOException>(() => backend.CreateDirectory(path));
    }
}
