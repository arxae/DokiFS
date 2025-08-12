using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendMoveDirectoryTests
{
    [Fact(DisplayName = "MoveDirectory: Should move directory")]
    public void ShouldMoveDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/moveSource";
        VPath destPath = $"/moveDest";

        backend.CreateDirectory(sourcePath);

        Assert.True(backend.Exists(sourcePath));
        Assert.False(backend.Exists(destPath));

        backend.MoveDirectory(sourcePath, destPath);

        Assert.False(backend.Exists(sourcePath));
        Assert.True(backend.Exists(destPath));
    }
}
