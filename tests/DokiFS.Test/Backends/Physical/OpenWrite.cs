using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendOpenWriteTests : IDisposable
{
    readonly IoTestUtilities util;

    public PhysicalFileSystemBackendOpenWriteTests()
    {
        util = new(nameof(PhysicalFileSystemBackendOpenWriteTests));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "OpenWrite: Opens valid filestream")]
    public void ShouldOpenValidFileStrean()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string file = "test.txt";
        VPath path = $"/{file}";
        string content = "test";

        Assert.False(util.FileExists(file));

        using Stream stream = backend.OpenWrite(path);

        // Assert the file is created
        Assert.True(util.FileExists(file));
        Assert.True(stream.CanWrite);
        Assert.Equal(0, stream.Length);

        // Assert the file can be written to with std stream means
        using StreamWriter w = new(stream);
        w.Write(content);

        w.Dispose();
        stream.Dispose();

        string readContent = util.GetContentString(file);
        Assert.Equal(content, readContent);
    }
}
