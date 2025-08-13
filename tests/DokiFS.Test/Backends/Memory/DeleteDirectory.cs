using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendDeleteDirectoryTests
{
    [Fact(DisplayName = "DeleteDirectory: Deletes directory")]
    public void ShouldDeleteDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test";

        backend.CreateDirectory(path);
        Assert.True(backend.Exists(path));

        backend.DeleteDirectory(path);

        Assert.False(backend.Exists(path));
    }

    [Fact(DisplayName = "DeleteDirectory: Throws exception on path to file")]
    public void ShouldThrowExceptionOnFilePath()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test";

        backend.CreateFile(path);

        Assert.Throws<DirectoryNotFoundException>(() => backend.DeleteDirectory(path));
    }
}
