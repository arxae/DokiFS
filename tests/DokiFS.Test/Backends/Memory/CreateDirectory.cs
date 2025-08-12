using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendCreateDirectoryTests
{
    [Fact(DisplayName = "CreateDirectory: Creates directory")]
    public void ShouldCreateDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test";

        backend.CreateDirectory(path);

        Assert.True(backend.Exists(path));
    }

    [Fact(DisplayName = "CreateDirectory: Throws exception on path to file")]
    public void ShouldReturnOnFilePath()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test";

        backend.CreateFile(path);

        Exception? exception = Record.Exception(() => backend.CreateDirectory(path));

        Assert.Null(exception);
    }
}
