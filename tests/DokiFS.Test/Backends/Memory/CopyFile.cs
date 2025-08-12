using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendCopyFileTests
{
    [Fact(DisplayName = "CopyFile: Copy File")]
    public void CopyFile()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/source.txt";
        VPath destPath = $"/dest.txt";

        backend.CreateFile(sourcePath);

        Assert.True(backend.Exists(sourcePath));

        backend.CopyFile(sourcePath, destPath);

        Assert.True(backend.Exists(sourcePath));
        Assert.True(backend.Exists(destPath));
    }

    [Fact(DisplayName = "CopyFile: Source does not exist")]
    public void CopyFileSourceNotExist()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/notExist";
        VPath destPath = $"/dest.txt";

        Assert.False(backend.Exists(sourcePath));

        Assert.Throws<FileNotFoundException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "CopyFile: Source exists and is directory")]
    public void CopyFileSourceIsDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/folder";
        VPath destPath = $"/dest.txt";

        backend.CreateDirectory(sourcePath);

        Assert.True(backend.Exists(sourcePath));

        Assert.Throws<IOException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "CopyFile: Destination exists and is directory")]
    public void CopyFileDestinationIsDirectory()
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
