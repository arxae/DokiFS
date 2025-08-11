using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendOpenReadTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendOpenReadTests()
    {
        util = new(nameof(PhysicalFileSystemBackendOpenReadTests));
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "OpenStrean: Should open stream")]
    public void ShouldOpenStream()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "file.txt";
        VPath sourcePath = $"/{source}";

        util.CreateTempFileWithSize(source, 1024);
        Assert.True(util.FileExists(source));

        using Stream stream = backend.OpenRead(sourcePath);

        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        Assert.Equal(1024, stream.Length);
    }

    [Fact(DisplayName = "OpenRead: Open non-existing file throws exception")]
    public void OpenReadNonExistingFile()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);
        VPath source = "/noexist.txt";

        Assert.Throws<FileNotFoundException>(() => backend.OpenRead(source));
    }

    [Fact(DisplayName = "OpenRead: File exists but is a directory throws exception")]
    public void OpenReadDirectory()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string path = "test";
        VPath dir = $"/{path}";
        util.CreateTempDirectory(path);

        Assert.True(util.DirExists(path));

        Assert.Throws<IOException>(() => backend.OpenRead(path));
    }
}
