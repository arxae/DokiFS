using DokiFS.Backends.Memory;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendExistsTests
{
    [Fact(DisplayName = "Exists: Basic check")]
    public void ExistsBasicCheck()
    {
        MemoryFileSystemBackend backed = new();

        VPath rootFile = "/testFile.txt";
        VPath subDir = "/testDir";
        VPath subDirFile = "/testDir/testFile.txt";

        backed.CreateFile(rootFile);
        backed.CreateDirectory(subDir);
        backed.CreateFile(subDirFile);

        Assert.True(backed.Exists("/testFile.txt"));
        Assert.True(backed.Exists("/testDir"));
        Assert.True(backed.Exists("/testDir/testFile.txt"));
    }

    [Fact(DisplayName = "Exists: Non existing")]
    public void ExistsNonExisting()
    {
        MemoryFileSystemBackend backend = new();

        Assert.False(backend.Exists("/nonexisting.txt"));
    }
}
