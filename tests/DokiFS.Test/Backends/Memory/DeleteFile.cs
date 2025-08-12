using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendDeleteFileTests
{
    [Fact(DisplayName = "DeleteFile: Basic Delete File")]
    public void ShouldDeleteFile()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test.txt";

        backend.CreateFile(path);

        Assert.True(backend.Exists(path));

        backend.DeleteFile(path);

        Assert.False(backend.Exists(path));
    }

    [Fact(DisplayName = "DeleteFile: Delete non-existing file")]
    public void DeleteNonExistingFileThrowsException()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/test.txt";

        Assert.False(backend.Exists(path));

        _ = Assert.Throws<FileNotFoundException>(() => backend.DeleteFile(path));
    }
}
