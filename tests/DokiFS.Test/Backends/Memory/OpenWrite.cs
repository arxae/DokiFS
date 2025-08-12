using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendOpenWriteTests
{
    [Fact(DisplayName = "OpenWrite: Opens valid filestream")]
    public void ShouldOpenValidFileStrean()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test.txt";
        string content = "test";

        Assert.False(backend.Exists(path));

        using Stream stream = backend.OpenWrite(path);

        // Assert the file is created
        Assert.True(backend.Exists(path));
        Assert.True(stream.CanWrite);
        Assert.Equal(0, stream.Length);

        // Assert the file can be written to with std stream means
        using StreamWriter w = new(stream);
        w.Write(content);

        w.Dispose();
        stream.Dispose();

        using StreamReader r = new(backend.OpenRead(path));
        string readContent = r.ReadToEnd();
        r.Dispose();

        Assert.Equal(content, readContent);
    }
}
