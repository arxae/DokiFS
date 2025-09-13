using DokiFS.Backends.Archive.Zip;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendConstructorTests
{
    [Fact(DisplayName = "Constructor: Archive does not exist")]
    public void ArchiveDoesNotExist()
    {
        string path = string.Empty;
        Assert.Throws<FileNotFoundException>(() => new ZipArchiveFileSystemBackend(path));
    }
}
