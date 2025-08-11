using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendDeleteFileTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendDeleteFileTests()
    {
        util = new("CreateFile");
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

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
}
