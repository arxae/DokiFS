using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendOpenReadTests
{
    [Fact(DisplayName = "OpenStrean: Should open stream")]
    public void ShouldOpenStream()
    {
        MemoryFileSystemBackend backend = new();

        VPath sourcePath = $"/file.txt";

        backend.CreateFile(sourcePath, 1024);
        Assert.True(backend.Exists(sourcePath));

        using Stream stream = backend.OpenRead(sourcePath);

        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        Assert.Equal(1024, stream.Length);
    }

    [Fact(DisplayName = "OpenRead: Open non-existing file throws exception")]
    public void OpenReadNonExistingFile()
    {
        MemoryFileSystemBackend backend = new();
        VPath source = "/noexist.txt";

        Assert.Throws<FileNotFoundException>(() => backend.OpenRead(source));
    }

    [Fact(DisplayName = "OpenRead: File exists but is a directory throws exception")]
    public void OpenReadDirectory()
    {
        MemoryFileSystemBackend backend = new();

        VPath dirPath = $"/test";

        backend.CreateDirectory(dirPath);

        Assert.True(backend.Exists(dirPath));

        Assert.Throws<IOException>(() => backend.OpenRead(dirPath));
    }
}
