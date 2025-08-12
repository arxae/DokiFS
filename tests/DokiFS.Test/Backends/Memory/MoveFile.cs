using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendMoveFileTests
{
    [Fact(DisplayName = "MoveFile: Basic Move File")]
    public void ShouldMoveFile()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/source.txt";
        VPath destPath = $"/dest.txt";

        backend.CreateFile(sourcePath);

        Assert.True(backend.Exists(sourcePath));

        backend.MoveFile(sourcePath, destPath);

        Assert.False(backend.Exists(sourcePath));
        Assert.True(backend.Exists(destPath));
    }

    [Fact(DisplayName = "MoveFile: Source does not exist")]
    public void MoveFileSourceNotExist()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/notexist.txt";
        VPath destPath = $"/dest.txt";

        Assert.False(backend.Exists(sourcePath));

        Assert.Throws<FileNotFoundException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "MoveFile: Source exists and is directory")]
    public void MoveFileSourceIsDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/folder";
        VPath destPath = $"/dest.txt";

        backend.CreateDirectory(sourcePath);

        Assert.True(backend.Exists(sourcePath));

        Assert.Throws<IOException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "MoveFile: Destination exists and is directory")]
    public void MoveFileDestinationIsDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/source.txt";
        VPath destPath = $"/dest";

        backend.CreateFile(sourcePath);
        backend.CreateDirectory(destPath);

        Assert.True(backend.Exists(sourcePath));
        Assert.True(backend.Exists(destPath));

        Assert.Throws<IOException>(() => backend.MoveFile(sourcePath, destPath));
    }
}
