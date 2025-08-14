using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendCopyDirectoryTests
{
    [Fact(DisplayName = "CopyDirectory: Should copy directory and contents")]
    public void ShouldCopyDirectoryAndContents()
    {
        MemoryFileSystemBackend backend = new();

        VPath source = "/moveSource";
        VPath dirFile = source.Append("file1.txt");
        VPath destination = "/moveDest";

        backend.CreateDirectory(source);
        backend.CreateFile(dirFile);

        Assert.True(backend.Exists(source));
        Assert.True(backend.Exists(dirFile));
        Assert.False(backend.Exists(destination));


        backend.CopyDirectory(source, destination);

        Assert.True(backend.Exists(source));
        Assert.True(backend.Exists(dirFile));
        Assert.True(backend.Exists(destination));
    }
}
