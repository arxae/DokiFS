using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendDeleteDirectoryTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendDeleteDirectoryTests()
    {
        util = new(nameof(PhysicalFileSystemBackendDeleteDirectoryTests));
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

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

    // TODO: Check thrown exception. Seems that on windows it's an IOException and on mac a DirectoryNotFoundException
    // Should be ioexception, check on mac later
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
